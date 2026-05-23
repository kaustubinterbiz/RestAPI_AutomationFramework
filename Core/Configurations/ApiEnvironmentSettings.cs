namespace EnterpriseApiAutomationFramework.Core.Configurations;

/// <summary>
/// Base URLs and HTTP settings for the active environment.
/// Bound from appsettings.json → "ApiUrls" and "Timeout".
/// </summary>
public sealed class ApiEnvironmentSettings
{
    public string AuthBaseUrl { get; set; } = string.Empty;

    public string ApiBaseUrl { get; set; } = string.Empty;

    public int TimeoutMilliseconds { get; set; } = 30_000;
}
