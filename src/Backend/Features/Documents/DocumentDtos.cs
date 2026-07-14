namespace Backend.Features.Documents;

internal static class DocumentUploadHelper
{
    internal static bool IsAllowedExtension(string fileName) =>
        fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
        fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
}

internal static class DocumentDownloadHelper
{
    internal static string GetFileType(string title)
    {
        if (title.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            return "MD";

        if (title.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            return "TXT";

        return "TXT";
    }

    internal static (string FileName, string ContentType) GetDownloadMetadata(string title)
    {
        if (title.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            return (title, "text/markdown");

        if (title.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            return (title, "text/plain");

        return ($"{title}.txt", "text/plain");
    }
}
