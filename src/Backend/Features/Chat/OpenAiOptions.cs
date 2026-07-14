namespace Backend.Features.Chat;

public class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxMessagesPerSession { get; set; } = 40;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
