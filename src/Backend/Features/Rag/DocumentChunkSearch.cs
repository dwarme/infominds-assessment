using System.Text.Json;
using Backend.Features.Chat;

namespace Backend.Features.Rag;

public class DocumentChunkSearchRequest
{
    public string Query { get; set; } = "";
    public string? CustomerName { get; set; }
    public string? SupplierName { get; set; }
    public int? DocumentId { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
}

public class DocumentChunkSearchHit
{
    public int DocumentId { get; set; }
    public string DocumentTitle { get; set; } = "";
    public DateTime UploadedAt { get; set; }
    public int ChunkIndex { get; set; }
    public string Text { get; set; } = "";
    public float Score { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
}

public class DocumentChunkSearchResult
{
    public string Query { get; set; } = "";
    public int TotalCandidates { get; set; }
    public int ReturnedCount { get; set; }
    public string? Message { get; set; }
    public List<DocumentChunkSearchHit> Chunks { get; set; } = [];
}

public class DocumentChunkSearch(
    BackendContext context,
    OpenAiEmbeddingClient embeddingClient,
    IOptions<OpenAiOptions> openAiOptions,
    IOptions<RagOptions> ragOptions)
{
    public async Task<DocumentChunkSearchResult> SearchAsync(
        DocumentChunkSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        SearchQueryLimits.EnsureWithinLimit(request.Query, "Query");
        SearchQueryLimits.EnsureWithinLimit(request.CustomerName, "CustomerName");
        SearchQueryLimits.EnsureWithinLimit(request.SupplierName, "SupplierName");

        if (string.IsNullOrWhiteSpace(request.Query))
            throw new BadHttpRequestException("Query is required.");

        var trimmedQuery = request.Query.Trim();

        if (!openAiOptions.Value.IsConfigured)
        {
            return new DocumentChunkSearchResult
            {
                Query = trimmedQuery,
                Message = "OpenAI API key is not configured; document search is unavailable.",
            };
        }

        List<int>? customerIds = null;
        List<int>? supplierIds = null;

        if (request.CustomerId is int customerId)
            customerIds = [customerId];
        else if (!string.IsNullOrWhiteSpace(request.CustomerName))
        {
            var name = request.CustomerName.Trim().ToLower();
            customerIds = await context.Customers
                .Where(c => c.Name.ToLower().Contains(name))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            if (customerIds.Count == 0)
            {
                return new DocumentChunkSearchResult
                {
                    Query = trimmedQuery,
                    Message = $"No customers matched '{request.CustomerName.Trim()}'.",
                };
            }
        }

        if (request.SupplierId is int supplierId)
            supplierIds = [supplierId];
        else if (!string.IsNullOrWhiteSpace(request.SupplierName))
        {
            var name = request.SupplierName.Trim().ToLower();
            supplierIds = await context.Suppliers
                .Where(s => s.Name.ToLower().Contains(name))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            if (supplierIds.Count == 0)
            {
                return new DocumentChunkSearchResult
                {
                    Query = trimmedQuery,
                    Message = $"No suppliers matched '{request.SupplierName.Trim()}'.",
                };
            }
        }

        var query = context.DocumentChunks.AsNoTracking().AsQueryable();

        if (request.DocumentId is int documentId)
            query = query.Where(c => c.DocumentId == documentId);

        if (customerIds is not null)
            query = query.Where(c => c.CustomerId != null && customerIds.Contains(c.CustomerId.Value));

        if (supplierIds is not null)
            query = query.Where(c => c.SupplierId != null && supplierIds.Contains(c.SupplierId.Value));

        var candidates = await (
            from chunk in query
            join document in context.Documents.AsNoTracking() on chunk.DocumentId equals document.Id
            join customer in context.Customers.AsNoTracking() on chunk.CustomerId equals customer.Id into customers
            from customer in customers.DefaultIfEmpty()
            join supplier in context.Suppliers.AsNoTracking() on chunk.SupplierId equals supplier.Id into suppliers
            from supplier in suppliers.DefaultIfEmpty()
            select new
            {
                chunk.DocumentId,
                DocumentTitle = document.Title,
                document.UploadedAt,
                chunk.ChunkIndex,
                chunk.Text,
                chunk.EmbeddingJson,
                chunk.CustomerId,
                CustomerName = customer != null ? customer.Name : null,
                chunk.SupplierId,
                SupplierName = supplier != null ? supplier.Name : null,
            })
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return new DocumentChunkSearchResult
            {
                Query = trimmedQuery,
                TotalCandidates = 0,
                Message = "No indexed document chunks matched the filters.",
            };
        }

        var queryEmbedding = await embeddingClient.EmbedAsync(trimmedQuery, cancellationToken);
        var topK = Math.Max(1, ragOptions.Value.TopK);

        var ranked = candidates
            .Select(candidate =>
            {
                var embedding = JsonSerializer.Deserialize<float[]>(candidate.EmbeddingJson) ?? [];
                return new DocumentChunkSearchHit
                {
                    DocumentId = candidate.DocumentId,
                    DocumentTitle = candidate.DocumentTitle,
                    UploadedAt = candidate.UploadedAt,
                    ChunkIndex = candidate.ChunkIndex,
                    Text = candidate.Text,
                    Score = VectorMath.CosineSimilarity(queryEmbedding, embedding),
                    CustomerId = candidate.CustomerId,
                    CustomerName = candidate.CustomerName,
                    SupplierId = candidate.SupplierId,
                    SupplierName = candidate.SupplierName,
                };
            })
            .OrderByDescending(hit => hit.Score)
            .Take(topK)
            .ToList();

        return new DocumentChunkSearchResult
        {
            Query = trimmedQuery,
            TotalCandidates = candidates.Count,
            ReturnedCount = ranked.Count,
            Chunks = ranked,
        };
    }
}
