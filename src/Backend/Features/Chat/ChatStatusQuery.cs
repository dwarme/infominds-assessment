using Backend.Features.Rag;

namespace Backend.Features.Chat;

public class ChatStatusQuery : IRequest<IResult>;

internal class ChatStatusQueryHandler(
    IOptions<OpenAiOptions> openAiOptions,
    IOptions<RagOptions> ragOptions,
    BackendContext context)
    : IRequestHandler<ChatStatusQuery, IResult>
{
    public async Task<IResult> Handle(ChatStatusQuery request, CancellationToken cancellationToken)
    {
        var openAi = openAiOptions.Value;
        var rag = ragOptions.Value;
        var indexedChunkCount = await context.DocumentChunks.CountAsync(cancellationToken);

        return Results.Ok(new ChatStatusResponse
        {
            Configured = openAi.IsConfigured,
            Model = openAi.Model,
            EmbeddingModel = rag.EmbeddingModel,
            RagConfigured = openAi.IsConfigured,
            IndexedChunkCount = indexedChunkCount,
            TopK = rag.TopK,
        });
    }
}

public class ChatStatusResponse
{
    public bool Configured { get; set; }
    public string Model { get; set; } = "";
    public string EmbeddingModel { get; set; } = "";
    public bool RagConfigured { get; set; }
    public int IndexedChunkCount { get; set; }
    public int TopK { get; set; }
}
