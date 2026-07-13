namespace Backend.Features.Customers;

using Backend.Features.Documents;

public class CustomerDocumentDownloadQuery : IRequest<IResult>
{
    public int CustomerId { get; set; }
    public int DocumentId { get; set; }
}

internal class CustomerDocumentDownloadQueryHandler(BackendContext context)
    : IRequestHandler<CustomerDocumentDownloadQuery, IResult>
{
    public async Task<IResult> Handle(CustomerDocumentDownloadQuery request, CancellationToken cancellationToken)
    {
        var document = await context.Documents.SingleOrDefaultAsync(
            d => d.Id == request.DocumentId && d.CustomerId == request.CustomerId,
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
