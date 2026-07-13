namespace Backend.Features.Customers;

public class CustomerDetailQuery : IRequest<IResult>
{
    public int CustomerId { get; set; }
}

public class CustomerDetailQueryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Iban { get; set; } = "";

    public CustomerDetailQueryResponseCustomerCategory? CustomerCategory { get; set; }
}

public class CustomerDetailQueryResponseCustomerCategory
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
}

internal class CustomerDetailQueryHandler(BackendContext context)
    : IRequestHandler<CustomerDetailQuery, IResult>
{
    public async Task<IResult> Handle(CustomerDetailQuery request, CancellationToken cancellationToken)
    {
        var customer = await context.Customers.SingleOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (customer is null)
            return Results.NotFound();

        var result = new CustomerDetailQueryResponse
        {
            Id = customer.Id,
            Name = customer.Name,
            Address = customer.Address,
            Email = customer.Email,
            Phone = customer.Phone,
            Iban = customer.Iban,
            CustomerCategory = null,
        };

        if (customer.CustomerCategoryId is not null)
        {
            var category = await context.CustomerCategories.SingleOrDefaultAsync(
                c => c.Id == customer.CustomerCategoryId,
                cancellationToken);

            if (category is not null)
            {
                result.CustomerCategory = new CustomerDetailQueryResponseCustomerCategory
                {
                    Id = category.Id,
                    Code = category.Code,
                    Description = category.Description,
                };
            }
        }

        return Results.Ok(result);
    }
}
