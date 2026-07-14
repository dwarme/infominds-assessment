namespace Backend.Features.Customers;

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Iban { get; set; } = "";

    public CustomerCategoryDto? CustomerCategory { get; set; }

    public static CustomerDto From(Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        Address = customer.Address,
        Email = customer.Email,
        Phone = customer.Phone,
        Iban = customer.Iban,
        CustomerCategory = customer.CustomerCategory is null
            ? null
            : CustomerCategoryDto.From(customer.CustomerCategory),
    };
}

public class CustomerCategoryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";

    public static CustomerCategoryDto From(CustomerCategory category) => new()
    {
        Id = category.Id,
        Code = category.Code,
        Description = category.Description,
    };
}
