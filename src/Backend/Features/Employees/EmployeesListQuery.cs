namespace Backend.Features.Employees;

public class EmployeesListQuery : IRequest<EmployeesListQueryPaginatedResponse>
{
    public string? SearchText { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int? Page { get; set; } = 1;
    public int? PageSize { get; set; } = 50;
}

public class EmployeesListQueryPaginatedResponse
{
    public List<EmployeesListQueryResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class EmployeesListQueryResponse
{
    public int Id { get; set; }
    public string Code { get; internal set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Address { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public EmployeesListQueryResponseDepartment? Department { get; set; }
}

public class EmployeesListQueryResponseDepartment
{
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
}


internal class EmployeesListQueryHandler(BackendContext context) : IRequestHandler<EmployeesListQuery, EmployeesListQueryPaginatedResponse>
{
    private readonly BackendContext context = context;

    public async Task<EmployeesListQueryPaginatedResponse> Handle(EmployeesListQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = ListPagination.Normalize(request.Page, request.PageSize);

        SearchQueryLimits.EnsureWithinLimit(request.SearchText, "SearchText");
        SearchQueryLimits.EnsureWithinLimit(request.FirstName, "FirstName");
        SearchQueryLimits.EnsureWithinLimit(request.LastName, "LastName");

        var query = context.Employees.AsQueryable();
        if (!string.IsNullOrEmpty(request.SearchText))
        {
            var searchTextFormatted = request.SearchText.ToLower();
            query = query.Where(q =>
                q.FirstName.ToLower().Contains(searchTextFormatted) ||
                q.LastName.ToLower().Contains(searchTextFormatted) ||
                q.Code.ToLower().Contains(searchTextFormatted) ||
                q.Email.ToLower().Contains(searchTextFormatted));
        }
        else
        {
            if (!string.IsNullOrEmpty(request.FirstName))
                query = query.Where(q => q.FirstName.ToLower().Contains(request.FirstName.ToLower()));
            if (!string.IsNullOrEmpty(request.LastName))
                query = query.Where(q => q.LastName.ToLower().Contains(request.LastName.ToLower()));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = ListPagination.GetTotalPages(totalCount, pageSize);

        var data = await query
            .OrderBy(q => q.LastName)
            .ThenBy(q => q.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = new List<EmployeesListQueryResponse>();

        foreach (var item in data)
        {
            var resultItem = new EmployeesListQueryResponse
            {
                Id = item.Id,
                Code = item.Code,
                FirstName = item.FirstName,
                LastName = item.LastName,
                Address = item.Address,
                Email = item.Email,
                Phone = item.Phone,
            };

            var department = await context.Departments.SingleOrDefaultAsync(q => q.Id == item.DepartmentId, cancellationToken);
            if (department is not null)
                resultItem.Department = new EmployeesListQueryResponseDepartment
                {
                    Code = department.Code,
                    Description = department.Description
                };

            items.Add(resultItem);
        }

        return new EmployeesListQueryPaginatedResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
        };
    }
}
