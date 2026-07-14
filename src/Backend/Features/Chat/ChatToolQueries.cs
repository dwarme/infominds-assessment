using System.Text.Json;

namespace Backend.Features.Chat;

public class ChatToolsListQuery : IRequest<IResult>;

internal class ChatToolsListQueryHandler(ChatToolExecutor toolExecutor)
    : IRequestHandler<ChatToolsListQuery, IResult>
{
    public Task<IResult> Handle(ChatToolsListQuery request, CancellationToken cancellationToken)
    {
        var tools = toolExecutor.Definitions
            .Select(tool => new
            {
                tool.Name,
                tool.Description,
            });

        return Task.FromResult(Results.Ok(tools));
    }
}

public class ChatToolInvokeQuery : IRequest<IResult>
{
    public string ToolName { get; set; } = "";
    public JsonElement Arguments { get; set; }
}

internal class ChatToolInvokeQueryHandler(ChatToolExecutor toolExecutor)
    : IRequestHandler<ChatToolInvokeQuery, IResult>
{
    public async Task<IResult> Handle(ChatToolInvokeQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ToolName))
            return Results.BadRequest(new { error = "ToolName is required." });

        try
        {
            var result = await toolExecutor.ExecuteAsync(
                request.ToolName.Trim(),
                request.Arguments.ValueKind == JsonValueKind.Undefined ? default : request.Arguments,
                cancellationToken);

            return Results.Content(result, "application/json");
        }
        catch (BadHttpRequestException exception)
        {
            return Results.BadRequest(new { error = exception.Message });
        }
    }
}
