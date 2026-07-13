namespace Backend.Features.Suppliers;

using Backend.Features.Documents;

public class SupplierDocumentContentQuery : IRequest<IResult>
{
    public int SupplierId { get; set; }
    public int DocumentId { get; set; }
}

public class SupplierDocumentContentQueryResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string FileType { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime UploadedAt { get; set; }
}

internal class SupplierDocumentContentQueryHandler(BackendContext context)
    : IRequestHandler<SupplierDocumentContentQuery, IResult>
{
    public async Task<IResult> Handle(SupplierDocumentContentQuery request, CancellationToken cancellationToken)
    {
        var document = await context.Documents.SingleOrDefaultAsync(
            d => d.Id == request.DocumentId && d.SupplierId == request.SupplierId,
            cancellationToken);

        if (document is null)
            return Results.NotFound();

        return Results.Ok(new SupplierDocumentContentQueryResponse
        {
            Id = document.Id,
            Title = document.Title,
            FileType = DocumentDownloadHelper.GetFileType(document.Title),
            Content = document.Content,
            UploadedAt = document.UploadedAt,
        });
    }
}
