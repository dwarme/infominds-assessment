using Backend.Features.Rag;

namespace Backend.Features.Chat;

public class ChatDocumentTools(BackendContext context, DocumentChunkSearch chunkSearch)
{
    public async Task<object> ListDocumentsForCustomerAsync(string customerName, CancellationToken cancellationToken)
    {
        SearchQueryLimits.EnsureWithinLimit(customerName, "customerName");
        if (string.IsNullOrWhiteSpace(customerName))
            throw new BadHttpRequestException("customerName is required.");

        var name = customerName.Trim().ToLower();
        var customers = await context.Customers
            .Where(c => c.Name.ToLower().Contains(name))
            .OrderBy(c => c.Name)
            .Take(ChatDataTools.MaxResults)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);

        if (customers.Count == 0)
        {
            return new
            {
                customerName = customerName.Trim(),
                customers,
                documents = Array.Empty<object>(),
                message = "No customers matched that name.",
            };
        }

        var customerIds = customers.Select(c => c.Id).ToList();
        var customerNames = customers.ToDictionary(c => c.Id, c => c.Name);
        var documents = await context.Documents
            .Where(d => d.CustomerId != null && customerIds.Contains(d.CustomerId.Value))
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);

        return new
        {
            customerName = customerName.Trim(),
            customers,
            totalCount = documents.Count,
            documents = documents.Select(d => new
            {
                d.Id,
                d.Title,
                d.UploadedAt,
                d.CustomerId,
                CustomerName = d.CustomerId is int id && customerNames.TryGetValue(id, out var n) ? n : null,
            }),
        };
    }

    public async Task<object> ListDocumentsForSupplierAsync(string supplierName, CancellationToken cancellationToken)
    {
        SearchQueryLimits.EnsureWithinLimit(supplierName, "supplierName");
        if (string.IsNullOrWhiteSpace(supplierName))
            throw new BadHttpRequestException("supplierName is required.");

        var name = supplierName.Trim().ToLower();
        var suppliers = await context.Suppliers
            .Where(s => s.Name.ToLower().Contains(name))
            .OrderBy(s => s.Name)
            .Take(ChatDataTools.MaxResults)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync(cancellationToken);

        if (suppliers.Count == 0)
        {
            return new
            {
                supplierName = supplierName.Trim(),
                suppliers,
                documents = Array.Empty<object>(),
                message = "No suppliers matched that name.",
            };
        }

        var supplierIds = suppliers.Select(s => s.Id).ToList();
        var supplierNames = suppliers.ToDictionary(s => s.Id, s => s.Name);
        var documents = await context.Documents
            .Where(d => d.SupplierId != null && supplierIds.Contains(d.SupplierId.Value))
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);

        return new
        {
            supplierName = supplierName.Trim(),
            suppliers,
            totalCount = documents.Count,
            documents = documents.Select(d => new
            {
                d.Id,
                d.Title,
                d.UploadedAt,
                d.SupplierId,
                SupplierName = d.SupplierId is int id && supplierNames.TryGetValue(id, out var n) ? n : null,
            }),
        };
    }

    public async Task<object> SearchDocumentChunksAsync(
        string query,
        string? customerName,
        string? supplierName,
        int? documentId,
        CancellationToken cancellationToken)
    {
        return await chunkSearch.SearchAsync(
            new DocumentChunkSearchRequest
            {
                Query = query,
                CustomerName = customerName,
                SupplierName = supplierName,
                DocumentId = documentId,
            },
            cancellationToken);
    }
}
