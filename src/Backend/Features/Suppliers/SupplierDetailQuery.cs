namespace Backend.Features.Suppliers;

public class SupplierDetailQuery : IRequest<IResult>
{
    public int SupplierId { get; set; }
}

internal class SupplierDetailQueryHandler(BackendContext context)
    : IRequestHandler<SupplierDetailQuery, IResult>
{
    public async Task<IResult> Handle(SupplierDetailQuery request, CancellationToken cancellationToken)
    {
        var supplier = await context.Suppliers.SingleOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken);
        if (supplier is null)
            return Results.NotFound();

        return Results.Ok(SupplierDto.From(supplier));
    }
}
