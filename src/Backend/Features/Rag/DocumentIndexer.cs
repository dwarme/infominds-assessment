using System.Text.Json;
using Backend.Features.Chat;

namespace Backend.Features.Rag;

/// <summary>
/// Chunks a document, embeds the chunks, and persists them to <c>DocumentChunks</c>.
/// Idempotent: replaces any existing chunks for the document.
/// </summary>
public class DocumentIndexer(
    BackendContext context,
    DocumentChunker chunker,
    OpenAiEmbeddingClient embeddingClient,
    IOptions<OpenAiOptions> openAiOptions,
    ILogger<DocumentIndexer> logger)
{
    private static readonly JsonSerializerOptions EmbeddingJsonOptions = new();

    public async Task<bool> IndexDocumentAsync(int documentId, CancellationToken cancellationToken = default)
    {
        if (!openAiOptions.Value.IsConfigured)
        {
            logger.LogWarning(
                "Skipping RAG index for document {DocumentId}: OpenAI API key is not configured.",
                documentId);
            return false;
        }

        var document = await context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null)
        {
            logger.LogWarning("Skipping RAG index: document {DocumentId} was not found.", documentId);
            return false;
        }

        await ReplaceChunksAsync(document, cancellationToken);
        return true;
    }

    /// <summary>
    /// Indexes every document that currently has no chunks (e.g. seeded docs after first deploy).
    /// </summary>
    public async Task<int> IndexUnindexedDocumentsAsync(CancellationToken cancellationToken = default)
    {
        if (!openAiOptions.Value.IsConfigured)
        {
            logger.LogWarning("Skipping RAG backfill: OpenAI API key is not configured.");
            return 0;
        }

        var unindexedIds = await context.Documents
            .Where(d => !context.DocumentChunks.Any(c => c.DocumentId == d.Id))
            .OrderBy(d => d.Id)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        if (unindexedIds.Count == 0)
        {
            logger.LogInformation("RAG backfill: all documents already indexed.");
            return 0;
        }

        logger.LogInformation("RAG backfill: indexing {Count} documents without chunks.", unindexedIds.Count);

        var indexed = 0;
        foreach (var documentId in unindexedIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (await IndexDocumentAsync(documentId, cancellationToken))
                    indexed++;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "RAG backfill failed for document {DocumentId}; continuing with remaining documents.",
                    documentId);
            }
        }

        logger.LogInformation("RAG backfill complete: indexed {Indexed} of {Total} documents.", indexed, unindexedIds.Count);
        return indexed;
    }

    private async Task ReplaceChunksAsync(Document document, CancellationToken cancellationToken)
    {
        var existing = await context.DocumentChunks
            .Where(c => c.DocumentId == document.Id)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
            context.DocumentChunks.RemoveRange(existing);

        var texts = chunker.Chunk(document.Content);
        if (texts.Count == 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "RAG index for document {DocumentId}: no chunks (empty content).",
                document.Id);
            return;
        }

        var embeddings = await embeddingClient.EmbedBatchAsync(texts, cancellationToken);

        for (var i = 0; i < texts.Count; i++)
        {
            context.DocumentChunks.Add(new DocumentChunk
            {
                DocumentId = document.Id,
                ChunkIndex = i,
                Text = texts[i],
                EmbeddingJson = JsonSerializer.Serialize(embeddings[i], EmbeddingJsonOptions),
                CustomerId = document.CustomerId,
                SupplierId = document.SupplierId,
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "RAG indexed document {DocumentId} into {ChunkCount} chunks.",
            document.Id,
            texts.Count);
    }
}
