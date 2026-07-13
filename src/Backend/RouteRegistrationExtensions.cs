using Backend.Features.Employees;
using Backend.Features.Suppliers;
using Backend.Features.Customers;

namespace Backend;

static class RouteRegistrationExtensions
{
    public static void UseApiRoutes(this WebApplication app)
    {
        var apiGroup = app.MapGroup("api");

        apiGroup.MapGet("suppliers/list", async ([AsParameters] SupplierListQuery query, IMediator mediator) => await mediator.Send(query))
                    .WithName("GetSuppliersList")
                    .WithOpenApi();

        apiGroup.MapGet("employees/list", async ([AsParameters] EmployeesListQuery query, IMediator mediator) => await mediator.Send(query))
                    .WithName("GetEmployeesList")
                    .WithOpenApi();

        apiGroup.MapGet("customers/list", async ([AsParameters] CustomersListQuery query, IMediator mediator) => await mediator.Send(query))
                    .WithName("GetCustomersList")
                    .WithOpenApi();

        apiGroup.MapGet("customers/{customerId}", async ([AsParameters] CustomerDetailQuery query, IMediator mediator) => await mediator.Send(query))
                    .WithName("GetCustomerById")
                    .WithOpenApi();

        apiGroup.MapGet("customers/{customerId}/documents", async ([AsParameters] CustomerDocumentsListQuery query, IMediator mediator) => await mediator.Send(query))
                    .WithName("ListCustomerDocuments")
                    .WithOpenApi();

        apiGroup.MapGet("customers/{customerId}/documents/{documentId}/content", async ([AsParameters] CustomerDocumentContentQuery query, IMediator mediator) => await mediator.Send(query))
                    .WithName("GetCustomerDocumentContent")
                    .WithOpenApi();

    }
}
