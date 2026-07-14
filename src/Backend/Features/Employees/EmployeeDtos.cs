namespace Backend.Features.Employees;

public class EmployeeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Address { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";

    public DepartmentDto? Department { get; set; }

    public static EmployeeDto From(Employee employee) => new()
    {
        Id = employee.Id,
        Code = employee.Code,
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        Address = employee.Address,
        Email = employee.Email,
        Phone = employee.Phone,
        Department = employee.Department is null
            ? null
            : DepartmentDto.From(employee.Department),
    };
}

public class DepartmentDto
{
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";

    public static DepartmentDto From(Department department) => new()
    {
        Code = department.Code,
        Description = department.Description,
    };
}
