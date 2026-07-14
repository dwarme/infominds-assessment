namespace Backend.Features.Customers;

using Backend.Features.Documents;

public class CustomerDocumentUploadQuery : IRequest<IResult>
{
    public int CustomerId { get; set; }
    public IFormFile File { get; set; } = null!;
}

internal class CustomerDocumentUploadQueryHandler(BackendContext context)
    : IRequestHandler<CustomerDocumentUploadQuery, IResult>
{
    public async Task<IResult> Handle(CustomerDocumentUploadQuery request, CancellationToken cancellationToken)
    {
        var customerExists = await context.Customers.AnyAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (!customerExists)
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
            CustomerId = request.CustomerId,
            UploadedAt = DateTime.UtcNow,
        };

        context.Documents.Add(document);
        await context.SaveChangesAsync(cancellationToken);

        var response = new CustomerDocumentsListQueryResponseItem
        {
            Id = document.Id,
            Title = document.Title,
            FileType = DocumentDownloadHelper.GetFileType(document.Title),
            UploadedAt = document.UploadedAt,
        };

        return Results.Created(
            $"/api/customers/{request.CustomerId}/documents/{document.Id}",
            response);
    }
}
