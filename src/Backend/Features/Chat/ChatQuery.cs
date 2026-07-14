namespace Backend.Features.Chat;

public class ChatQuery : IRequest<IResult>
{
    public Guid? ConversationId { get; set; }
    public string Message { get; set; } = "";
}

internal class ChatQueryHandler(IOptions<OpenAiOptions> options)
    : IRequestHandler<ChatQuery, IResult>
{
    public Task<IResult> Handle(ChatQuery request, CancellationToken cancellationToken)
    {
        if (!options.Value.IsConfigured)
        {
            return Task.FromResult(Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Chat service unavailable",
                detail: "OpenAI API key is not configured. Set OPENAI_API_KEY in .env.local or environment variables."));
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Task.FromResult(Results.BadRequest(new { error = "Message is required." }));
        }

        return Task.FromResult(Results.Json(new ChatResponse
        {
            ConversationId = request.ConversationId ?? Guid.NewGuid(),
            Reply = "Chat is not implemented yet. OpenAI configuration is ready.",
        }));
    }
}

public class ChatResponse
{
    public Guid ConversationId { get; set; }
    public string Reply { get; set; } = "";
}
