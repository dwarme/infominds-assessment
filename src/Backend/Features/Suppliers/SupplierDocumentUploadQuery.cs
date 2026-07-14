namespace Backend.Features.Suppliers;

using Backend.Features.Documents;

public class SupplierDocumentUploadQuery : IRequest<IResult>
{
    public int SupplierId { get; set; }
    public IFormFile File { get; set; } = null!;
}

internal class SupplierDocumentUploadQueryHandler(BackendContext context)
    : IRequestHandler<SupplierDocumentUploadQuery, IResult>
{
    public async Task<IResult> Handle(SupplierDocumentUploadQuery request, CancellationToken cancellationToken)
    {
        var supplierExists = await context.Suppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken);
        if (!supplierExists)
            return Results.NotFound();

        if (request.File.Length == 0)
            return Results.BadRequest(new { error = "File is required." });

        var title = Path.GetFileName(request.File.FileName);
        if (!DocumentUploadHelper.IsAllowedExtension(title))
            return Results.BadRequest(new { error = "Only .txt and .md files are allowed." });

        using var reader = new StreamReader(request.File.OpenReadStream());
        var content = await reader.ReadToEndAsync(cancellationToken);

        var document = new Document
        {
            Title = title,
            Content = content,
            SupplierId = request.SupplierId,
            UploadedAt = DateTime.UtcNow,
        };

        context.Documents.Add(document);
        await context.SaveChangesAsync(cancellationToken);

        var response = new SupplierDocumentsListQueryResponseItem
        {
            Id = document.Id,
            Title = document.Title,
            FileType = DocumentDownloadHelper.GetFileType(document.Title),
            UploadedAt = document.UploadedAt,
        };

        return Results.Created(
            $"/api/suppliers/{request.SupplierId}/documents/{document.Id}",
            response);
    }
}
