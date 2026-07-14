using System.Collections.Concurrent;

namespace Backend.Features.Chat;

public class ChatSessionStore
{
    private readonly ConcurrentDictionary<Guid, List<ChatMessage>> sessions = new();

    public bool TryGetSession(Guid conversationId, out IReadOnlyList<ChatMessage> messages)
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

    public void AddMessage(Guid conversationId, ChatMessage message)
    {
        var storedMessages = sessions.GetOrAdd(conversationId, _ => []);
        lock (storedMessages)
        {
            storedMessages.Add(message);
        }
    }
}
