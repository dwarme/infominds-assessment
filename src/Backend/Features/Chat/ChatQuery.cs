namespace Backend.Features.Chat;

public class ChatQuery : IRequest<IResult>
{
    public Guid? ConversationId { get; set; }
    public string Message { get; set; } = "";
}

internal class ChatQueryHandler(
    IOptions<OpenAiOptions> options,
    ChatSessionStore sessionStore,
    OpenAiChatClient openAiChatClient)
    : IRequestHandler<ChatQuery, IResult>
{
    public async Task<IResult> Handle(ChatQuery request, CancellationToken cancellationToken)
    {
        if (!options.Value.IsConfigured)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Chat service unavailable",
                detail: "OpenAI API key is not configured. Set OPENAI_API_KEY in .env.local or environment variables.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
            return Results.BadRequest(new { error = "Message is required." });

        SearchQueryLimits.EnsureWithinLimit(request.Message, "Message");

        var conversationId = ResolveConversationId(request.ConversationId);
        if (conversationId is null)
            return Results.NotFound(new { error = "Conversation not found." });

        sessionStore.EnsureSystemMessage(conversationId.Value);
        sessionStore.TrimHistory(conversationId.Value, options.Value.MaxMessagesPerSession);
        sessionStore.AddMessage(conversationId.Value, new OpenAiChatMessageRecord
        {
            Role = ChatMessageRoles.User,
            Content = request.Message.Trim(),
        });

        if (!sessionStore.TryGetSession(conversationId.Value, out var messages))
            return Results.NotFound(new { error = "Conversation not found." });

        try
        {
            var mutableMessages = messages.ToList();
            var reply = await openAiChatClient.CompleteAsync(mutableMessages, cancellationToken);
            sessionStore.ReplaceMessages(conversationId.Value, mutableMessages);

            return Results.Json(new ChatResponse
            {
                ConversationId = conversationId.Value,
                Reply = reply,
            });
        }
        catch (Exception exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Chat request failed",
                detail: exception.Message);
        }
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
