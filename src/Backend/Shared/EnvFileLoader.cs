namespace Backend.Shared;

public static class EnvFileLoader
{
    public static void Load(string path)
    {
        if (!File.Exists(path))
            return;

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();
            if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
                value = value[1..^1];

            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
