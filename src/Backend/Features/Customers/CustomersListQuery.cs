namespace Backend.Features.Customers;

/// <summary>
/// Query parameters for the customers list endpoint.
/// Bound from query string via [AsParameters] on GET /api/customers/list.
/// </summary>
public class CustomersListQuery : IRequest<CustomersListQueryPaginatedResponse>
{
    /// <summary>Filters customers by name or email (case-insensitive, partial match).</summary>
    public string? SearchText { get; set; }

    /// <summary>1-based page number. Defaults to 1.</summary>
    public int? Page { get; set; } = 1;

    /// <summary>Number of items per page. Defaults to 50, capped at 100.</summary>
    public int? PageSize { get; set; } = 50;
}

/// <summary>Paginated wrapper returned by the customers list endpoint.</summary>
public class CustomersListQueryPaginatedResponse
{
    public List<CustomerDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

internal class CustomersListQueryHandler(BackendContext context) : IRequestHandler<CustomersListQuery, CustomersListQueryPaginatedResponse>
{
    public async Task<CustomersListQueryPaginatedResponse> Handle(CustomersListQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = ListPagination.Normalize(request.Page, request.PageSize);

        SearchQueryLimits.EnsureWithinLimit(request.SearchText, "SearchText");

        var query = context.Customers.AsQueryable();
        if (!string.IsNullOrEmpty(request.SearchText))
        {
            var searchTextFormatted = request.SearchText.ToLower();
            query = query.Where(q =>
                q.Name.ToLower().Contains(searchTextFormatted) ||
                q.Email.ToLower().Contains(searchTextFormatted));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = ListPagination.GetTotalPages(totalCount, pageSize);

        var data = await query
            .Include(c => c.CustomerCategory)
            .OrderBy(q => q.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new CustomersListQueryPaginatedResponse
        {
            Items = data.Select(CustomerDto.From).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
        };
    }
}
