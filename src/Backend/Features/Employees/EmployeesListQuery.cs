namespace Backend.Features.Employees;

public class EmployeesListQuery : IRequest<List<EmployeesListQueryResponse>>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
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


internal class EmployeesListQueryHandler(BackendContext context) : IRequestHandler<EmployeesListQuery, List<EmployeesListQueryResponse>>
{
    private readonly BackendContext context = context;

    public async Task<List<EmployeesListQueryResponse>> Handle(EmployeesListQuery request, CancellationToken cancellationToken)
    {
        var query = context.Employees.AsQueryable();
        if (!string.IsNullOrEmpty(request.FirstName))
            query = query.Where(q => q.FirstName.ToLower().Contains(request.FirstName.ToLower()));
        if (!string.IsNullOrEmpty(request.LastName))
            query = query.Where(q => q.LastName.ToLower().Contains(request.LastName.ToLower()));

        var data = await query.OrderBy(q => q.LastName).ThenBy(q => q.FirstName).ToListAsync(cancellationToken);
        var result = new List<EmployeesListQueryResponse>();

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


            result.Add(resultItem);
        }

        return result;
    }
}