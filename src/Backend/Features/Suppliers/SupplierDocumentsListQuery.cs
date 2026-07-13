namespace Backend.Features.Suppliers;

using Backend.Features.Documents;

public class SupplierDocumentsListQuery : IRequest<IResult>
{
    public int SupplierId { get; set; }
}

public class SupplierDocumentsListQueryResponse
{
    public List<SupplierDocumentsListQueryResponseItem> Items { get; set; } = [];
}

public class SupplierDocumentsListQueryResponseItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string FileType { get; set; } = "";
    public DateTime UploadedAt { get; set; }
}

internal class SupplierDocumentsListQueryHandler(BackendContext context)
    : IRequestHandler<SupplierDocumentsListQuery, IResult>
{
    public async Task<IResult> Handle(SupplierDocumentsListQuery request, CancellationToken cancellationToken)
    {
        var supplierExists = await context.Suppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken);
        if (!supplierExists)
            return Results.NotFound();

        var documents = await context.Documents
            .Where(d => d.SupplierId == request.SupplierId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new { d.Id, d.Title, d.UploadedAt })
            .ToListAsync(cancellationToken);

        // Metadata only — Content is loaded via GET .../documents/{id}/content
        var items = documents.Select(d => new SupplierDocumentsListQueryResponseItem
        {
            Id = d.Id,
            Title = d.Title,
            FileType = DocumentDownloadHelper.GetFileType(d.Title),
            UploadedAt = d.UploadedAt,
        }).ToList();

        return Results.Ok(new SupplierDocumentsListQueryResponse { Items = items });
    }
}
