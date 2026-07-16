namespace Backend.Features.Customers;

using Backend.Features.Documents;
using Backend.Features.Rag;

public class CustomerDocumentUploadQuery : IRequest<IResult>
{
    public int CustomerId { get; set; }
    public IFormFile File { get; set; } = null!;
}

internal class CustomerDocumentUploadQueryHandler(
    BackendContext context,
    DocumentIndexer documentIndexer,
    ILogger<CustomerDocumentUploadQueryHandler> logger)
    : IRequestHandler<CustomerDocumentUploadQuery, IResult>
{
    public async Task<IResult> Handle(CustomerDocumentUploadQuery request, CancellationToken cancellationToken)
    {
        var customerExists = await context.Customers.AnyAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (!customerExists)
            return Results.NotFound();

        if (request.File.Length == 0)
            return Results.BadRequest(new { error = "File is required." });

        if (!DocumentUploadHelper.IsWithinSizeLimit(request.File.Length))
            return Results.BadRequest(new { error = DocumentUploadHelper.MaxFileSizeError });

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

        try
        {
            await documentIndexer.IndexDocumentAsync(document.Id, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Document {DocumentId} was saved but RAG indexing failed.",
                document.Id);
        }

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
