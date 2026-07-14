namespace Backend.Features.Chat;

public class ChatQuery : IRequest<IResult>
{
    public Guid? ConversationId { get; set; }
    public string Message { get; set; } = "";
}

internal class ChatQueryHandler(IOptions<OpenAiOptions> options, ChatSessionStore sessionStore)
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

        var conversationId = ResolveConversationId(request.ConversationId);
        if (conversationId is null)
        {
            return Task.FromResult(Results.NotFound(new
            {
                error = "Conversation not found.",
            }));
        }

        sessionStore.AddMessage(conversationId.Value, new ChatMessage
        {
            Role = ChatMessageRoles.User,
            Content = request.Message.Trim(),
            Timestamp = DateTimeOffset.UtcNow,
        });

        var history = sessionStore.TryGetSession(conversationId.Value, out var messages)
            ? messages
            : [];

        var userTurnCount = history.Count(message => message.Role == ChatMessageRoles.User);
        var reply = $"Received turn {userTurnCount}. This conversation has {history.Count} messages stored. LLM integration is coming in Phase 3.";

        sessionStore.AddMessage(conversationId.Value, new ChatMessage
        {
            Role = ChatMessageRoles.Assistant,
            Content = reply,
            Timestamp = DateTimeOffset.UtcNow,
        });

        return Task.FromResult(Results.Json(new ChatResponse
        {
            ConversationId = conversationId.Value,
            Reply = reply,
        }));
    }

    private Guid? ResolveConversationId(Guid? conversationId)
    {
        if (conversationId is null)
            return sessionStore.CreateSession();

        if (sessionStore.TryGetSession(conversationId.Value, out _))
            return conversationId.Value;

        return null;
    }
}

public class ChatResponse
{
    public Guid ConversationId { get; set; }
    public string Reply { get; set; } = "";
}
