namespace Backend.Features.Customers;

using Backend.Features.Documents;

public class CustomerDocumentsListQuery : IRequest<IResult>
{
    public int CustomerId { get; set; }
}

public class CustomerDocumentsListQueryResponse
{
    public List<CustomerDocumentsListQueryResponseItem> Items { get; set; } = [];
}

public class CustomerDocumentsListQueryResponseItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string FileType { get; set; } = "";
    public DateTime UploadedAt { get; set; }
}

internal class CustomerDocumentsListQueryHandler(BackendContext context)
    : IRequestHandler<CustomerDocumentsListQuery, IResult>
{
    public async Task<IResult> Handle(CustomerDocumentsListQuery request, CancellationToken cancellationToken)
    {
        var customerExists = await context.Customers.AnyAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (!customerExists)
            return Results.NotFound();

        var documents = await context.Documents
            .Where(d => d.CustomerId == request.CustomerId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new { d.Id, d.Title, d.UploadedAt })
            .ToListAsync(cancellationToken);

        // Metadata only — Content is loaded via GET .../documents/{id}/content
        var items = documents.Select(d => new CustomerDocumentsListQueryResponseItem
        {
            Id = d.Id,
            Title = d.Title,
            FileType = DocumentDownloadHelper.GetFileType(d.Title),
            UploadedAt = d.UploadedAt,
        }).ToList();

        return Results.Ok(new CustomerDocumentsListQueryResponse { Items = items });
    }
}
