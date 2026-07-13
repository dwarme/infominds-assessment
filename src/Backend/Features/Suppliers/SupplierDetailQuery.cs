namespace Backend.Features.Suppliers;

public class SupplierDetailQuery : IRequest<IResult>
{
    public int SupplierId { get; set; }
}

public class SupplierDetailQueryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
}

internal class SupplierDetailQueryHandler(BackendContext context)
    : IRequestHandler<SupplierDetailQuery, IResult>
{
    public async Task<IResult> Handle(SupplierDetailQuery request, CancellationToken cancellationToken)
    {
        var supplier = await context.Suppliers.SingleOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken);
        if (supplier is null)
            return Results.NotFound();

        return Results.Ok(new SupplierDetailQueryResponse
        {
            Id = supplier.Id,
            Name = supplier.Name,
            Address = supplier.Address,
            Email = supplier.Email,
            Phone = supplier.Phone,
        });
    }
}
