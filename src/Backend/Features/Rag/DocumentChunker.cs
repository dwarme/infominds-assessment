namespace Backend.Features.Rag;

/// <summary>
/// Splits document text into overlapping chunks for embedding and retrieval.
/// Paragraph-first (blank-line separated), then merges up to <see cref="RagOptions.ChunkSize"/>;
/// long paragraphs are hard-split. Each new chunk starts with the last
/// <see cref="RagOptions.ChunkOverlap"/> characters of the previous one.
/// </summary>
public class DocumentChunker(IOptions<RagOptions> options)
{
    public IReadOnlyList<string> Chunk(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return [];

        var chunkSize = Math.Max(1, options.Value.ChunkSize);
        var overlap = Math.Clamp(options.Value.ChunkOverlap, 0, chunkSize - 1);

        var normalized = content.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        if (normalized.Length == 0)
            return [];

        var paragraphs = SplitParagraphs(normalized);
        var chunks = new List<string>();
        string? previousChunk = null;
        var current = "";

        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length > chunkSize)
            {
                Emit(ref current, chunks, ref previousChunk);
                HardSplit(paragraph, chunkSize, overlap, chunks, ref previousChunk);
                continue;
            }

            if (current.Length == 0)
            {
                current = WithOverlapPrefix(previousChunk, overlap, paragraph);
                if (current.Length > chunkSize)
                    current = paragraph;
                continue;
            }

            if (current.Length + 2 + paragraph.Length <= chunkSize)
            {
                current += "\n\n" + paragraph;
                continue;
            }

            Emit(ref current, chunks, ref previousChunk);
            current = WithOverlapPrefix(previousChunk, overlap, paragraph);
            if (current.Length > chunkSize)
                current = paragraph;
        }

        Emit(ref current, chunks, ref previousChunk);
        return chunks;
    }

    private static List<string> SplitParagraphs(string text)
    {
        var parts = text.Split("\n\n", StringSplitOptions.None);
        var paragraphs = new List<string>(parts.Length);

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.Length > 0)
                paragraphs.Add(trimmed);
        }

        return paragraphs;
    }

    private static void Emit(ref string current, List<string> chunks, ref string? previousChunk)
    {
        if (string.IsNullOrWhiteSpace(current))
        {
            current = "";
            return;
        }

        var emitted = current.Trim();
        chunks.Add(emitted);
        previousChunk = emitted;
        current = "";
    }

    private static void HardSplit(
        string text,
        int chunkSize,
        int overlap,
        List<string> chunks,
        ref string? previousChunk)
    {
        var offset = 0;
        while (offset < text.Length)
        {
            var prefix = TakeOverlap(previousChunk, overlap);
            var capacity = chunkSize - prefix.Length;
            if (capacity <= 0)
            {
                prefix = "";
                capacity = chunkSize;
            }

            var take = Math.Min(capacity, text.Length - offset);
            var chunk = prefix.Length == 0
                ? text.Substring(offset, take)
                : prefix + text.Substring(offset, take);

            chunks.Add(chunk);
            previousChunk = chunk;
            offset += take;
        }
    }

    private static string WithOverlapPrefix(string? previousChunk, int overlap, string next)
    {
        var prefix = TakeOverlap(previousChunk, overlap);
        if (prefix.Length == 0)
            return next;

        return prefix + "\n\n" + next;
    }

    private static string TakeOverlap(string? text, int overlap)
    {
        if (overlap <= 0 || string.IsNullOrEmpty(text))
            return "";

        return text.Length <= overlap
            ? text
            : text[^overlap..];
    }
}
