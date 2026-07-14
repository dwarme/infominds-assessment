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
    public List<CustomersListQueryResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class CustomersListQueryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Iban { get; set; } = "";

    public CustomersListQueryResponseCustomerCategory? CustomerCategory { get; set; }
}

public class CustomersListQueryResponseCustomerCategory
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
}


internal class CustomersListQueryHandler(BackendContext context) : IRequestHandler<CustomersListQuery, CustomersListQueryPaginatedResponse>
{
    private readonly BackendContext context = context;

    public async Task<CustomersListQueryPaginatedResponse> Handle(CustomersListQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = ListPagination.Normalize(request.Page, request.PageSize);

        SearchQueryLimits.EnsureWithinLimit(request.SearchText, "SearchText");

        var query = context.Customers.AsQueryable();
        if (!string.IsNullOrEmpty(request.SearchText)) {
            var searchTextFormatted = request.SearchText.ToLower();
            query = query.Where(q => q.Name.ToLower().Contains(searchTextFormatted) || q.Email.ToLower().Contains(searchTextFormatted) );
        }

        // Count before paging so the client can build pagination controls.
        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = ListPagination.GetTotalPages(totalCount, pageSize);

        var data = await query
            .OrderBy(q => q.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = new List<CustomersListQueryResponse>();

        foreach (var item in data)
        {
            var resultItem = new CustomersListQueryResponse
            {
                Id = item.Id,
                Name = item.Name,
                Address = item.Address,
                Email = item.Email,
                Phone = item.Phone,
                Iban = item.Iban,
                CustomerCategory = null,
            };

            // Category is optional; not all customers have one assigned.
            var customerCategory = await context.CustomerCategories.SingleOrDefaultAsync(q => q.Id == item.CustomerCategoryId, cancellationToken);
            if (customerCategory is not null) {
                resultItem.CustomerCategory = new CustomersListQueryResponseCustomerCategory
                {
                    Id = customerCategory.Id,
                    Code = customerCategory.Code,
                    Description = customerCategory.Description
                };
            }

            items.Add(resultItem);
        }

        return new CustomersListQueryPaginatedResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
        };
    }
}
