using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Authentication;

/// <summary>
/// Scenario-scoped bearer token (AsyncLocal) for safe parallel execution.
/// Optional persistence to appsettings when PersistAccessToken is true.
/// </summary>
public static class TokenManager
{
    private static readonly AsyncLocal<string?> ScenarioToken = new();

    public static bool HasToken => !string.IsNullOrWhiteSpace(ScenarioToken.Value);

    public static string AccessToken => ScenarioToken.Value ?? string.Empty;

    public static void InitializeFromConfig(string appSettingsFile = "appsettings.json")
    {
        var cached = ConfigReaderNew.GetJsonValue(appSettingsFile, "access_token");
        if (!string.IsNullOrWhiteSpace(cached))
        {
            ScenarioToken.Value = cached;
        }
    }

    public static void SetAccessToken(string token, string appSettingsFile = "appsettings.json")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ScenarioToken.Value = token;

        if (AppConfiguration.GetBool("PersistAccessToken"))
        {
            ConfigReaderNew.UpdateJsonValue(appSettingsFile, "access_token", token);
        }
    }

    public static void Clear() => ScenarioToken.Value = null;

    /// <summary>Call at scenario start so parallel workers do not share tokens.</summary>
    public static void ResetForNewScenario()
    {
        ScenarioToken.Value = null;

        if (AppConfiguration.GetBool("UseCachedAccessToken"))
        {
            InitializeFromConfig();
        }
    }
}
