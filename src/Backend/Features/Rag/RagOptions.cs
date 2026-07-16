namespace Backend.Features.Rag;

/// <summary>
/// Tunable RAG settings. Embeddings reuse the same OpenAI API key / base URL as chat
/// (<see cref="Backend.Features.Chat.OpenAiOptions"/>).
/// </summary>
public class RagOptions
{
    public const string SectionName = "Rag";

    /// <summary>Target max characters per chunk before starting a new one.</summary>
    public int ChunkSize { get; set; } = 800;

    /// <summary>Characters of the previous chunk prepended to the next (boundary continuity).</summary>
    public int ChunkOverlap { get; set; } = 100;

    /// <summary>Number of most similar chunks returned per semantic search.</summary>
    public int TopK { get; set; } = 5;

    /// <summary>OpenAI embeddings model id.</summary>
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    /// <summary>Expected embedding vector length for text-embedding-3-small.</summary>
    public int EmbeddingDimensions { get; set; } = 1536;
}
