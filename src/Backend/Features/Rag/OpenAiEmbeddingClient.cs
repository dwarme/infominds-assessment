using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Backend.Features.Chat;

namespace Backend.Features.Rag;

/// <summary>
/// Calls OpenAI Embeddings API using the same API key / base URL as chat.
/// </summary>
public class OpenAiEmbeddingClient(
    HttpClient httpClient,
    IOptions<OpenAiOptions> openAiOptions,
    IOptions<RagOptions> ragOptions)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default) =>
        EmbedOneOrThrowAsync(text, cancellationToken);

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
            return [];

        for (var i = 0; i < texts.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(texts[i]))
                throw new ArgumentException($"Embedding input at index {i} is empty.", nameof(texts));
        }

        var settings = openAiOptions.Value;
        if (!settings.IsConfigured)
            throw new InvalidOperationException("OpenAI API key is not configured.");

        var rag = ragOptions.Value;
        var requestBody = new
        {
            model = rag.EmbeddingModel,
            input = texts.ToArray(),
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{settings.BaseUrl.TrimEnd('/')}/embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"OpenAI embeddings request failed ({(int)response.StatusCode}): {ParseOpenAiError(responseJson)}");

        return ParseEmbeddings(responseJson, texts.Count, rag.EmbeddingDimensions);
    }

    private async Task<float[]> EmbedOneOrThrowAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Embedding input is empty.", nameof(text));

        var results = await EmbedBatchAsync([text], cancellationToken);
        return results[0];
    }

    private static IReadOnlyList<float[]> ParseEmbeddings(string responseJson, int expectedCount, int expectedDimensions)
    {
        using var document = JsonDocument.Parse(responseJson);
        if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("OpenAI embeddings response is missing the data array.");

        var byIndex = new SortedDictionary<int, float[]>();
        foreach (var item in data.EnumerateArray())
        {
            var index = item.TryGetProperty("index", out var indexElement) && indexElement.TryGetInt32(out var parsedIndex)
                ? parsedIndex
                : byIndex.Count;

            if (!item.TryGetProperty("embedding", out var embeddingElement) || embeddingElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("OpenAI embeddings response item is missing embedding values.");

            var values = new float[embeddingElement.GetArrayLength()];
            var i = 0;
            foreach (var value in embeddingElement.EnumerateArray())
                values[i++] = value.GetSingle();

            if (expectedDimensions > 0 && values.Length != expectedDimensions)
            {
                throw new InvalidOperationException(
                    $"OpenAI embedding dimension mismatch: expected {expectedDimensions}, got {values.Length}.");
            }

            byIndex[index] = values;
        }

        if (byIndex.Count != expectedCount)
        {
            throw new InvalidOperationException(
                $"OpenAI embeddings count mismatch: expected {expectedCount}, got {byIndex.Count}.");
        }

        return byIndex.Values.ToList();
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
