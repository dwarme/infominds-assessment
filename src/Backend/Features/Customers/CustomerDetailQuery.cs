namespace Backend.Features.Customers;

public class CustomerDetailQuery : IRequest<IResult>
{
    public int CustomerId { get; set; }
}

internal class CustomerDetailQueryHandler(BackendContext context)
    : IRequestHandler<CustomerDetailQuery, IResult>
{
    public async Task<IResult> Handle(CustomerDetailQuery request, CancellationToken cancellationToken)
    {
        var customer = await context.Customers
            .Include(c => c.CustomerCategory)
            .SingleOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer is null)
            return Results.NotFound();

        return Results.Ok(CustomerDto.From(customer));
    }
}
