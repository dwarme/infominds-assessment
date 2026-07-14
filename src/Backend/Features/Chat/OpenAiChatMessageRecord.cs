namespace Backend.Features.Chat;

public class OpenAiChatMessageRecord
{
    public string Role { get; set; } = "";
    public string? Content { get; set; }
    public string? ToolCallId { get; set; }
    public string? Name { get; set; }
    public List<OpenAiToolCallRecord>? ToolCalls { get; set; }
}

public class OpenAiToolCallRecord
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string ArgumentsJson { get; set; } = "{}";
}
