namespace Backend.Features.Chat;

public class ChatStatusQuery : IRequest<IResult>;

internal class ChatStatusQueryHandler(IOptions<OpenAiOptions> options)
    : IRequestHandler<ChatStatusQuery, IResult>
{
    public Task<IResult> Handle(ChatStatusQuery request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        return Task.FromResult(Results.Ok(new ChatStatusResponse
        {
            Configured = settings.IsConfigured,
            Model = settings.Model,
        }));
    }
}

public class ChatStatusResponse
{
    public bool Configured { get; set; }
    public string Model { get; set; } = "";
}
