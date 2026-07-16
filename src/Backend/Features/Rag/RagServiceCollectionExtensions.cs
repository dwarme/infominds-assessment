namespace Backend.Features.Rag;

using Backend.Features.Chat;

public static class RagServiceCollectionExtensions
{
    public static IServiceCollection AddRagServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RagOptions>()
            .Configure(options =>
            {
                configuration.GetSection(RagOptions.SectionName).Bind(options);

                options.EmbeddingModel = FirstNonEmpty(
                    configuration["OPENAI_EMBEDDING_MODEL"],
                    configuration[$"{RagOptions.SectionName}:EmbeddingModel"],
                    options.EmbeddingModel) ?? "text-embedding-3-small";

                if (options.ChunkSize <= 0)
                    options.ChunkSize = 800;
                if (options.ChunkOverlap < 0)
                    options.ChunkOverlap = 0;
                if (options.ChunkOverlap >= options.ChunkSize)
                    options.ChunkOverlap = Math.Max(0, options.ChunkSize / 8);
                if (options.TopK <= 0)
                    options.TopK = 5;
                if (options.EmbeddingDimensions <= 0)
                    options.EmbeddingDimensions = 1536;
            });

        // Embedding client, chunker, indexer, and search are registered in later RAG phases.
        return services;
    }

    /// <summary>
    /// Embeddings are available when the shared OpenAI API key is configured.
    /// </summary>
    public static bool IsEmbeddingConfigured(OpenAiOptions openAiOptions) =>
        openAiOptions.IsConfigured;

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
