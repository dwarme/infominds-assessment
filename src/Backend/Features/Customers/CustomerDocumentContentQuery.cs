namespace Backend.Features.Customers;

using Backend.Features.Documents;

public class CustomerDocumentContentQuery : IRequest<IResult>
{
    public int CustomerId { get; set; }
    public int DocumentId { get; set; }
}

public class CustomerDocumentContentQueryResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string FileType { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime UploadedAt { get; set; }
}

internal class CustomerDocumentContentQueryHandler(BackendContext context)
    : IRequestHandler<CustomerDocumentContentQuery, IResult>
{
    public async Task<IResult> Handle(CustomerDocumentContentQuery request, CancellationToken cancellationToken)
    {
        var document = await context.Documents.SingleOrDefaultAsync(
            d => d.Id == request.DocumentId && d.CustomerId == request.CustomerId,
            cancellationToken);

        if (document is null)
            return Results.NotFound();

        return Results.Ok(new CustomerDocumentContentQueryResponse
        {
            Id = document.Id,
            Title = document.Title,
            FileType = DocumentDownloadHelper.GetFileType(document.Title),
            Content = document.Content,
            UploadedAt = document.UploadedAt,
        });
    }
}
