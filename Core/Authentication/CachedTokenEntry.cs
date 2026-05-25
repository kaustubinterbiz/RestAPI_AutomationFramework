namespace EnterpriseApiAutomationFramework.Core.Authentication;

public sealed class CachedTokenEntry
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public string LoginRoleKey { get; set; } = string.Empty;

    public string Environment { get; set; } = string.Empty;
}
