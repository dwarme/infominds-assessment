using Backend.Features.Chat;
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

        apiGroup.MapGet("customers/{customerId}/documents/{documentId}", async ([AsParameters] CustomerDocumentDownloadQuery query, IMediator mediator) => await mediator.Send(query))
                    .WithName("DownloadCustomerDocument")
                    .WithOpenApi();

        apiGroup.MapPost("customers/{customerId}/documents", async (int customerId, IFormFile file, IMediator mediator) =>
                    await mediator.Send(new CustomerDocumentUploadQuery { CustomerId = customerId, File = file }))
                    .WithName("UploadCustomerDocument")
                    .WithOpenApi()
                    .DisableAntiforgery();

        apiGroup.MapGet("suppliers/{supplierId}", async ([AsParameters] SupplierDetailQuery query, IMediator mediator) => await mediator.Send(query))
                    .WithName("GetSupplierById")
                    .WithOpenApi();

        apiGroup.MapGet("suppliers/{supplierId}/documents", async ([AsParameters] SupplierDocumentsListQuery query, IMediator mediator) => await mediator.Send(query))
                    .WithName("ListSupplierDocuments")
                    .WithOpenApi();

        apiGroup.MapGet("suppliers/{supplierId}/documents/{documentId}/content", async ([AsParameters] SupplierDocumentContentQuery query, IMediator mediator) => await mediator.Send(query))
                    .WithName("GetSupplierDocumentContent")
                    .WithOpenApi();

        apiGroup.MapGet("suppliers/{supplierId}/documents/{documentId}", async ([AsParameters] SupplierDocumentDownloadQuery query, IMediator mediator) => await mediator.Send(query))
                    .WithName("DownloadSupplierDocument")
                    .WithOpenApi();

        apiGroup.MapPost("suppliers/{supplierId}/documents", async (int supplierId, IFormFile file, IMediator mediator) =>
                    await mediator.Send(new SupplierDocumentUploadQuery { SupplierId = supplierId, File = file }))
                    .WithName("UploadSupplierDocument")
                    .WithOpenApi()
                    .DisableAntiforgery();

        apiGroup.MapGet("chat/status", async (IMediator mediator) => await mediator.Send(new ChatStatusQuery()))
                    .WithName("GetChatStatus")
                    .WithOpenApi();

        apiGroup.MapPost("chat", async (ChatQuery query, IMediator mediator) => await mediator.Send(query))
                    .WithName("PostChat")
                    .WithOpenApi();

        apiGroup.MapGet("chat/tools", async (IMediator mediator) => await mediator.Send(new ChatToolsListQuery()))
                    .WithName("ListChatTools")
                    .WithOpenApi();

        apiGroup.MapPost("chat/tools/invoke", async (ChatToolInvokeQuery query, IMediator mediator) => await mediator.Send(query))
                    .WithName("InvokeChatTool")
                    .WithOpenApi();

    }
}
