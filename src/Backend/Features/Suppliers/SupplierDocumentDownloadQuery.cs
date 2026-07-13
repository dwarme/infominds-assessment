namespace Backend.Features.Suppliers;

using Backend.Features.Documents;

public class SupplierDocumentDownloadQuery : IRequest<IResult>
{
    public int SupplierId { get; set; }
    public int DocumentId { get; set; }
}

internal class SupplierDocumentDownloadQueryHandler(BackendContext context)
    : IRequestHandler<SupplierDocumentDownloadQuery, IResult>
{
    public async Task<IResult> Handle(SupplierDocumentDownloadQuery request, CancellationToken cancellationToken)
    {
        var document = await context.Documents.SingleOrDefaultAsync(
            d => d.Id == request.DocumentId && d.SupplierId == request.SupplierId,
            cancellationToken);

        if (document is null)
            return Results.NotFound();

        var (fileName, contentType) = DocumentDownloadHelper.GetDownloadMetadata(document.Title);

        return Results.File(
            System.Text.Encoding.UTF8.GetBytes(document.Content),
            contentType,
            fileName);
    }
}
