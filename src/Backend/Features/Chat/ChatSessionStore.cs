using System.Collections.Concurrent;

namespace Backend.Features.Chat;

public class ChatSessionStore
{
    private readonly ConcurrentDictionary<Guid, List<OpenAiChatMessageRecord>> sessions = new();

    public bool TryGetSession(Guid conversationId, out IReadOnlyList<OpenAiChatMessageRecord> messages)
    {
        if (!sessions.TryGetValue(conversationId, out var storedMessages))
        {
            messages = [];
            return false;
        }

        lock (storedMessages)
        {
            messages = storedMessages.ToList();
        }

        return true;
    }

    public Guid CreateSession()
    {
        var conversationId = Guid.NewGuid();
        sessions.TryAdd(conversationId, []);
        return conversationId;
    }

    public void EnsureSystemMessage(Guid conversationId)
    {
        var storedMessages = sessions.GetOrAdd(conversationId, _ => []);
        lock (storedMessages)
        {
            if (storedMessages.Any(message => message.Role == ChatMessageRoles.System))
                return;

            storedMessages.Add(new OpenAiChatMessageRecord
            {
                Role = ChatMessageRoles.System,
                Content = ChatSystemPrompt.Text,
            });
        }
    }

    public void TrimHistory(Guid conversationId, int maxMessages)
    {
        if (maxMessages < 2)
            return;

        var storedMessages = sessions.GetOrAdd(conversationId, _ => []);
        lock (storedMessages)
        {
            if (storedMessages.Count <= maxMessages)
                return;

            var systemMessages = storedMessages
                .Where(message => message.Role == ChatMessageRoles.System)
                .ToList();

            var conversationMessages = storedMessages
                .Where(message => message.Role != ChatMessageRoles.System)
                .TakeLast(maxMessages - systemMessages.Count)
                .ToList();

            storedMessages.Clear();
            storedMessages.AddRange(systemMessages);
            storedMessages.AddRange(conversationMessages);
        }
    }

    public void ReplaceMessages(Guid conversationId, IReadOnlyList<OpenAiChatMessageRecord> messages)
    {
        var storedMessages = sessions.GetOrAdd(conversationId, _ => []);
        lock (storedMessages)
        {
            storedMessages.Clear();
            storedMessages.AddRange(messages);
        }
    }

    public void AddMessage(Guid conversationId, OpenAiChatMessageRecord message)
    {
        var storedMessages = sessions.GetOrAdd(conversationId, _ => []);
        lock (storedMessages)
        {
            storedMessages.Add(message);
        }
    }
}
