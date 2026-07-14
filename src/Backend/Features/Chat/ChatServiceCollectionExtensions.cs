namespace Backend.Features.Chat;

public static class ChatServiceCollectionExtensions
{
    public static IServiceCollection AddChatServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OpenAiOptions>()
            .Configure(options =>
            {
                configuration.GetSection(OpenAiOptions.SectionName).Bind(options);

                options.ApiKey = FirstNonEmpty(
                    configuration["OPENAI_API_KEY"],
                    configuration[$"{OpenAiOptions.SectionName}:ApiKey"],
                    options.ApiKey);

                options.Model = FirstNonEmpty(
                    configuration["OPENAI_MODEL"],
                    configuration[$"{OpenAiOptions.SectionName}:Model"],
                    options.Model) ?? "gpt-4o-mini";

                options.BaseUrl = FirstNonEmpty(
                    configuration["OPENAI_BASE_URL"],
                    configuration[$"{OpenAiOptions.SectionName}:BaseUrl"],
                    options.BaseUrl) ?? "https://api.openai.com/v1";
            });

        services.AddSingleton<ChatSessionStore>();
        services.AddScoped<ChatDataTools>();
        services.AddScoped<ChatToolExecutor>();

        return services;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}
