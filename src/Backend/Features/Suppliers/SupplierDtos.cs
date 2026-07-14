namespace Backend.Features.Suppliers;

public class SupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";

    public static SupplierDto From(Supplier supplier) => new()
    {
        Id = supplier.Id,
        Name = supplier.Name,
        Address = supplier.Address,
        Email = supplier.Email,
        Phone = supplier.Phone,
    };
}
