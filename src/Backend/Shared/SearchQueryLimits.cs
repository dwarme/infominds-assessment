namespace Backend.Shared;

public static class SearchQueryLimits
{
    public const int MaxTextLength = 100;

    public static void EnsureWithinLimit(string? value, string parameterName)
    {
        if (value is not null && value.Length > MaxTextLength)
            throw new BadHttpRequestException($"{parameterName} must be at most {MaxTextLength} characters.");
    }
}
