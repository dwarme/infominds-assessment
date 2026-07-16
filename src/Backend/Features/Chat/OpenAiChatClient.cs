using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Backend.Features.Chat;

public class OpenAiChatClient(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ChatToolExecutor toolExecutor)
{
    private const int MaxToolRounds = 8;
    private const int MaxTokens = 1000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<string> CompleteAsync(
        IList<OpenAiChatMessageRecord> messages,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
            throw new InvalidOperationException("OpenAI API key is not configured.");

        for (var round = 0; round <= MaxToolRounds; round++)
        {
            using var response = await SendCompletionRequestAsync(settings, messages, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"OpenAI request failed ({(int)response.StatusCode}): {ParseOpenAiError(responseJson)}");

            using var document = JsonDocument.Parse(responseJson);
            var choice = document.RootElement.GetProperty("choices")[0];
            var message = choice.GetProperty("message");

            if (message.TryGetProperty("tool_calls", out var toolCallsElement) &&
                toolCallsElement.ValueKind == JsonValueKind.Array &&
                toolCallsElement.GetArrayLength() > 0)
            {
                if (round == MaxToolRounds)
                    throw new InvalidOperationException("Chat tool loop exceeded the maximum number of rounds.");

                var assistantMessage = ParseAssistantMessage(message, toolCallsElement);
                messages.Add(assistantMessage);

                foreach (var toolCall in assistantMessage.ToolCalls ?? [])
                {
                    string toolResult;
                    try
                    {
                        var arguments = string.IsNullOrWhiteSpace(toolCall.ArgumentsJson)
                            ? default
                            : JsonDocument.Parse(toolCall.ArgumentsJson).RootElement;

                        toolResult = await toolExecutor.ExecuteAsync(toolCall.Name, arguments, cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        toolResult = JsonSerializer.Serialize(new
                        {
                            error = exception.Message,
                            tool = toolCall.Name,
                            arguments = toolCall.ArgumentsJson,
                        });
                    }

                    messages.Add(new OpenAiChatMessageRecord
                    {
                        Role = ChatMessageRoles.Tool,
                        ToolCallId = toolCall.Id,
                        Name = toolCall.Name,
                        Content = toolResult,
                    });
                }

                continue;
            }

            var reply = message.TryGetProperty("content", out var contentElement)
                ? contentElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(reply))
                throw new InvalidOperationException("OpenAI returned an empty assistant response.");

            messages.Add(new OpenAiChatMessageRecord
            {
                Role = ChatMessageRoles.Assistant,
                Content = reply.Trim(),
            });

            return reply.Trim();
        }

        throw new InvalidOperationException("Chat completion ended without an assistant response.");
    }

    private async Task<HttpResponseMessage> SendCompletionRequestAsync(
        OpenAiOptions settings,
        IList<OpenAiChatMessageRecord> messages,
        CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = settings.Model,
            messages = messages.Select(BuildRequestMessage),
            tools = toolExecutor.Definitions.Select(definition => definition.ToOpenAiTool()),
            tool_choice = "auto",
            max_tokens = MaxTokens,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{settings.BaseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json");

        return await httpClient.SendAsync(request, cancellationToken);
    }

    private static object BuildRequestMessage(OpenAiChatMessageRecord message)
    {
        if (message.Role == ChatMessageRoles.Tool)
        {
            return new
            {
                role = message.Role,
                tool_call_id = message.ToolCallId,
                content = message.Content ?? "",
            };
        }

        if (message.ToolCalls is { Count: > 0 })
        {
            return new
            {
                role = message.Role,
                content = message.Content,
                tool_calls = message.ToolCalls.Select(toolCall => new
                {
                    id = toolCall.Id,
                    type = "function",
                    function = new
                    {
                        name = toolCall.Name,
                        arguments = toolCall.ArgumentsJson,
                    },
                }),
            };
        }

        return new
        {
            role = message.Role,
            content = message.Content ?? "",
        };
    }

    private static OpenAiChatMessageRecord ParseAssistantMessage(JsonElement message, JsonElement toolCallsElement)
    {
        var toolCalls = new List<OpenAiToolCallRecord>();
        foreach (var toolCall in toolCallsElement.EnumerateArray())
        {
            var function = toolCall.GetProperty("function");
            toolCalls.Add(new OpenAiToolCallRecord
            {
                Id = toolCall.GetProperty("id").GetString() ?? Guid.NewGuid().ToString(),
                Name = function.GetProperty("name").GetString() ?? "",
                ArgumentsJson = function.GetProperty("arguments").GetString() ?? "{}",
            });
        }

        return new OpenAiChatMessageRecord
        {
            Role = ChatMessageRoles.Assistant,
            Content = message.TryGetProperty("content", out var content) ? content.GetString() : null,
            ToolCalls = toolCalls,
        };
    }

    private static string ParseOpenAiError(string responseJson)
    {
        try
        {
            using var document = JsonDocument.Parse(responseJson);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
            {
                var text = message.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
        }
        catch (JsonException)
        {
        }

        return responseJson;
    }
}
