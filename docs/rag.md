# RAG — Design decisions & rationale

This document explains **what** we built for Retrieval-Augmented Generation over Customer/Supplier documents, and **why** each choice was made. It is the design journal for the RAG feature.

For day-to-day setup and usage, see the **RAG** section in [README.md](../README.md).

**Prerequisite:** AI chat (OpenAI tool calling + EF-backed tools).

---

## Problem

Users can already upload `.txt` / `.md` files linked to a Customer or Supplier. Chat can answer structured questions about customers and suppliers (counts, IBAN, email domains, etc.), but **not** questions about document *content* (contracts, delivery notes, etc.).

RAG closes that gap: index document text → retrieve relevant passages → feed them to the LLM as tool results.

---

## Guiding principles

1. **Extend the existing chat**, do not add a second AI service.
2. **Minimal new infrastructure** — one SQLite DB, one OpenAI key, one .NET process.
3. **Compose with existing patterns** — MediatR, EF Core, schema patches, `.env.local`, tool calling.
4. **Correct for assessment scale** — simple solutions that stay honest about limits.
5. **Document the why** — this file; README stays operational.

---

## Stack decisions (summary)

| Area | Choice | Why (short) |
|------|--------|-------------|
| Integration | New chat tools in the existing OpenAI tool loop | Same hybrid pattern as customer/supplier tools; debuggable via `/api/chat/tools/invoke` |
| Document source | Existing `Documents` table + upload APIs | Already validates `.txt`/`.md`, 1 MB, owner XOR; RAG indexes derived data |
| Chunking | ~800 chars, 100-char overlap, paragraph-aware | Enough context per chunk; overlap avoids cutting ideas; no token lib needed |
| Embeddings | OpenAI `text-embedding-3-small` (same API key) | One provider/config; cheap and fast; matches existing `OpenAiChatClient` style |
| Vector store | SQLite `DocumentChunks` + in-app cosine similarity | Embedded, stack-compatible; fine for hundreds of chunks |
| Top-k | 5 | Fits token budget; balances recall vs. noise |
| Indexing | Sync on upload + startup backfill (dev) | Immediate searchability; seeds the 50 fake docs |

---

## Why not alternatives

### Separate Python / FastAPI RAG service

Would need a second process, another language in Codespaces, proxy/CORS, and duplicated config. The assessment already has a working .NET chat stack — extending it is lower risk and easier to review.

### Auto-inject retrieved chunks into every chat message

Wastes tokens when the user asks structured questions ("how many customers in Garden?"). Tool calling lets the model **choose** when to search documents.

### Keyword-only search (`LIKE` / FTS5)

Would fail the semantic examples in the brief (e.g. "problemi di consegna" vs wording like "ritardi nella spedizione"). Embeddings are required for those paraphrases.

### Dedicated vector DB (Qdrant, Pinecone, Weaviate)

Extra service for ~50–500 documents. Spec asks for an **embedded** store compatible with the stack — SQLite fits.

### pgvector

Would force PostgreSQL; the app is SQLite-first.

### sqlite-vec / ANN indexes

Better at large scale; unnecessary friction in Codespaces for this corpus. Documented as a future swap if chunk count grows.

### Google `text-embedding-004` as primary

Valid free-tier option (AI Studio). We chose OpenAI so chat and embeddings share one key and one HTTP client pattern. Google remains a documented alternative if a zero-cost embedding tier is preferred later.

### Local embedding models (ONNX / sentence-transformers)

No API cost, but heavy downloads and CPU inference — poor fit for Codespaces assessment.

### Whole-document embeddings / one-chunk-per-line

Whole doc: too coarse, input limits, all-or-nothing retrieval. Per line: too noisy, many API calls, lost context. Fixed-size with overlap is the pragmatic middle.

---

## Architecture (target)

```text
Upload (.txt/.md)
    → Documents.Content (source of truth)
    → Chunk → Embed → DocumentChunks (index)

Chat question about documents
    → OpenAI tool call (list / search)
    → Embed query → cosine top-k chunks
    → Tool JSON → LLM final answer
```

Phases below map 1:1 to implementation order.

---

## Implementation phases

### Phase 0 — Configuration *(done)*

**Delivered:**

- `Features/Rag/RagOptions.cs` — `ChunkSize`, `ChunkOverlap`, `TopK`, `EmbeddingModel`, `EmbeddingDimensions`
- `Features/Rag/RagServiceCollectionExtensions.cs` — `AddRagServices()`
- Wired in `Program.cs`
- Defaults in `appsettings.json` under `"Rag"`
- `.env.example` notes optional `OPENAI_EMBEDDING_MODEL` (same `OPENAI_API_KEY`)

**Why Phase 0 is separate:** Same as chat Phase 0 — lock config and DI before DB/API work so later phases only add services, not rethink knobs.

**Why reuse OpenAI key:** Avoid a second secrets file and second provider for an assessment. Embeddings are "configured" iff `OPENAI_API_KEY` is set.

**Why these defaults:**

| Setting | Default | Why |
|---------|---------|-----|
| `ChunkSize` | 800 | ~150–200 tokens; several chunks fit in one tool result |
| `ChunkOverlap` | 100 | Continuity across boundaries without duplicating most of the text |
| `TopK` | 5 | Common RAG default; enough recall without flooding the prompt |
| `EmbeddingModel` | `text-embedding-3-small` | Quality/cost/latency sweet spot; 1536 dims |
| `EmbeddingDimensions` | 1536 | Matches that model; used later for validation |

---

### Phase 1 — `DocumentChunk` entity *(done)*

**Delivered:**

- `Infrastructure/Database/DocumentChunk.cs` — entity + EF configuration
- `BackendContext` — `DbSet<DocumentChunk>`
- `RegistrationExtensions.EnsureDocumentChunksTable()` — `CREATE TABLE IF NOT EXISTS` + indexes on existing DBs

**Schema:**

| Column | Role |
|--------|------|
| `Id` | PK |
| `DocumentId` | FK → `Documents`, **ON DELETE CASCADE** |
| `ChunkIndex` | 0-based order within the document (unique with `DocumentId`) |
| `Text` | Chunk content |
| `EmbeddingJson` | JSON `float[]` (filled in Phase 3–4) |
| `CustomerId` / `SupplierId` | Denormalized owner (XOR check, same as Documents) |

**Why denormalize owners:** Scoped search ("chunks for customer X") and cross-supplier questions without joining every time.

**Why keep full `Documents.Content`:** Source of truth for download/preview; chunks are a derived index that can be rebuilt.

**Why cascade delete:** If a document is removed later, orphaned vectors must go with it.

**Why `EmbeddingJson` as TEXT:** SQLite has no native float array type; JSON is simple to serialize/deserialize in C# without extra packages.

**Why unique `(DocumentId, ChunkIndex)`:** Prevents duplicate chunks when re-indexing; indexer can replace by document cleanly.

---

### Phase 2 — Chunking service *(done)*

**Delivered:**

- `Features/Rag/DocumentChunker.cs` — `Chunk(string?) → IReadOnlyList<string>`
- Registered as singleton in `AddRagServices()`

**Algorithm:**

1. Normalize line endings; trim; empty → `[]`
2. Split on blank lines (`\n\n`)
3. Merge paragraphs while length ≤ `ChunkSize` (default 800)
4. On overflow: emit chunk, start next with last `ChunkOverlap` chars (default 100) + new paragraph
5. Paragraphs longer than `ChunkSize`: hard-split into windows with the same overlap

**Why paragraph-first:** Matches natural structure of `.md` / `.txt` better than blind character slicing.

**Why not tiktoken:** Extra dependency for 1 MB text files; character budget is enough here.

**Why not LLM-based semantic chunking:** Costly/slow at index time for little gain at this scale.

**Why overlap is applied only when starting the next chunk:** Avoids emitting a trailing overlap-only fragment after the last real content.

---

### Phase 3 — Embedding client *(done)*

**Delivered:**

- `Features/Rag/OpenAiEmbeddingClient.cs`
  - `EmbedAsync(string)` — single text
  - `EmbedBatchAsync(IReadOnlyList<string>)` — many texts in one request
- Registered via `AddHttpClient<OpenAiEmbeddingClient>()` (same timeout as chat)

**Behavior:**

- Uses `OpenAiOptions.ApiKey` + `BaseUrl` (shared with chat)
- Model from `RagOptions.EmbeddingModel` (default `text-embedding-3-small`)
- Validates response count and optional dimension length (`EmbeddingDimensions`)
- Clear errors for missing key, empty input, or API failures

**Why same provider as chat:** One API key, one HTTP pattern, one failure mode for the assessment.

**Why batch:** One HTTP round-trip per document (all chunks) instead of one per chunk.

---

### Phase 4 — Indexing pipeline *(done)*

**Delivered:**

- `Features/Rag/DocumentIndexer.cs`
  - `IndexDocumentAsync` — replace chunks for one document (chunk → embed batch → save)
  - `IndexUnindexedDocumentsAsync` — backfill docs with zero chunks
- Upload hooks in `CustomerDocumentUploadQuery` / `SupplierDocumentUploadQuery` (after `SaveChanges`)
- Dev startup backfill in `RegistrationExtensions` after seed

**Behavior:**

- Idempotent: deletes existing chunks for the document, then writes fresh ones
- Copies `CustomerId` / `SupplierId` onto each chunk
- Missing API key → log warning, skip (upload still returns 201)
- Indexing exception on upload → log warning, document remains saved
- Backfill continues after per-document failures

**Why sync indexing:** Max file 1 MB; a few chunks embed quickly; document is searchable immediately. A queue would be production polish, not assessment-critical.

**Why backfill:** Seeded Bogus documents exist without chunks until indexed.

**Why skip quietly if no API key:** Upload must still succeed; indexing is best-effort when embeddings are available.

---

### Phase 5 — Vector search *(done)*

**Delivered:**

- `Features/Rag/VectorMath.cs` — `CosineSimilarity(float[], float[])`
- `Features/Rag/DocumentChunkSearch.cs` — request/hit/result types + `SearchAsync`
- Registered scoped in `AddRagServices()`

**Flow:**

1. Validate query (required, ≤ 100 chars via `SearchQueryLimits`)
2. Resolve optional `CustomerName` / `SupplierName` / ids / `DocumentId` filters
3. Load candidate chunks from SQLite (with document title + owner names)
4. Embed the query with `OpenAiEmbeddingClient`
5. Score each candidate with cosine similarity; return top-k (`RagOptions.TopK`, default 5)

**Result shape:** `query`, `totalCandidates`, `returnedCount`, optional `message`, `chunks[]` (text, score, document metadata, owner).

**Why brute-force cosine:** Milliseconds for hundreds/thousands of vectors in C#; zero new native deps; fits “embedded store compatible with the stack”.

**Why filter by name/id:** Supports scoped questions (“chunks for customer X”) and cross-supplier questions without always scanning irrelevant owners.

**Why reuse OpenAI for the query embedding:** Same vector space as indexed chunks (`text-embedding-3-small`).

### Phase 6 — Chat integration *(done)*

**Delivered:**

- `ChatDocumentTools` — list documents for customer/supplier; `search_document_chunks`
- Tool schemas + executor wiring (`list_documents_for_customer`, `list_documents_for_supplier`, `search_document_chunks`)
- System prompt rules for document vs CRM tools (cite title/date; prefer chunk search for report content)

**Why three tools:** List answers “what exists / which is latest?”; search answers “what does it say?”.

**Why keep existing customer/supplier tools:** Resolve CRM entities first when needed, then compose with document search.

---

### Phase 7 — README polish *(done)*

**Delivered:**

- Full operational **RAG** section in `README.md` (choices table, setup, limits, sample file, examples)
- `docs/rag-architecture.svg` embedded in README
- `GET /api/chat/status` extended with `embeddingModel`, `ragConfigured`, `indexedChunkCount`, `topK`
- Chat widget: document example chips + status line showing embedding model / chunk count
- This file remains the design-rationale source of truth

---

## Example questions (target)

- "Cosa diceva l'ultimo contratto del cliente X?"
- "Quali fornitori hanno menzionato problemi di consegna nei documenti?"
- "Riassumi i documenti del fornitore Y"

---

## Out of scope (intentional)

| Item | Why deferred |
|------|----------------|
| Document delete API / re-index on delete | No delete endpoint today |
| Background indexing queue | Sync is enough at this scale |
| sqlite-vec / ANN | Unnecessary until corpus grows |
| Second embedding provider in code | Documented alternative only |
| Frontend upload changes | Detail-page upload already exists |
| Streaming chat | Not part of current chat design |

