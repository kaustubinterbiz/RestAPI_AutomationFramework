using EnterpriseApiAutomationFramework.Core.Authentication;

namespace EnterpriseApiAutomationFramework.Core.Configurations;

public sealed class AuthenticationSettings
{
    public AuthenticationMode Mode { get; set; } = AuthenticationMode.Shared;

    public bool PersistAccessToken { get; set; } = true;

    public bool UseCachedAccessToken { get; set; } = true;

    /// <summary>File used for cross-process token reuse (parallel workers).</summary>
    public string TokenCacheFile { get; set; } = "TestData/.auth/token-cache.json";

    /// <summary>Refresh token this many minutes before JWT expiry.</summary>
    public int RefreshBufferMinutes { get; set; } = 5;
}
