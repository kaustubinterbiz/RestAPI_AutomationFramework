namespace EnterpriseApiAutomationFramework.Core.Clients;

public enum FlexibleTokenMode
{
    Cached,
    None,
    Empty,
    Garbage,
    Malformed,
    Current
}

public static class FlexibleTokenModeParser
{
    public static FlexibleTokenMode Parse(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "none" => FlexibleTokenMode.None,
            "empty" => FlexibleTokenMode.Empty,
            "garbage" => FlexibleTokenMode.Garbage,
            "malformed" => FlexibleTokenMode.Malformed,
            "current" => FlexibleTokenMode.Current,
            "cached" or "-" or "" => FlexibleTokenMode.Cached,
            _ => throw new ArgumentException(
                $"Unknown flexible token mode '{value}'. Expected: none, empty, garbage, malformed, current, cached.")
        };
}
