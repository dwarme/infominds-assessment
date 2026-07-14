namespace Backend.Features.Suppliers;

public class SupplierListQuery : IRequest<List<SupplierDto>>
{
    public string? Name { get; set; }
}

internal class SupplierListQueryHandler(BackendContext context) : IRequestHandler<SupplierListQuery, List<SupplierDto>>
{
    public async Task<List<SupplierDto>> Handle(SupplierListQuery request, CancellationToken cancellationToken)
    {
        SearchQueryLimits.EnsureWithinLimit(request.Name, "Name");

        var query = context.Suppliers.AsQueryable();
        if (!string.IsNullOrEmpty(request.Name))
            query = query.Where(q => q.Name.ToLower().Contains(request.Name.ToLower()));

        var data = await query.OrderBy(q => q.Name).ToListAsync(cancellationToken);

        return data.Select(SupplierDto.From).ToList();
    }
}
