using EnterpriseApiAutomationFramework.Core.Configurations;
using TechTalk.SpecFlow;

namespace EnterpriseApiAutomationFramework.Core.Authentication;

/// <summary>
/// Bearer token for the current scenario (AsyncLocal) backed by shared in-process cache when configured.
/// </summary>
public static class TokenManager
{
    private const string ScenarioContextTokenKey = "AccessToken";

    private static readonly AsyncLocal<string?> ScenarioToken = new();
    private static readonly AsyncLocal<ScenarioContext?> BoundScenario = new();

    public static bool HasToken => !string.IsNullOrWhiteSpace(GetAccessToken());

    public static string AccessToken => GetAccessToken();

    public static void BindScenario(ScenarioContext scenarioContext) =>
        BoundScenario.Value = scenarioContext;

    public static string GetAccessToken()
    {
        if (!string.IsNullOrWhiteSpace(ScenarioToken.Value))
        {
            return ScenarioToken.Value;
        }

        var context = BoundScenario.Value;
        if (context != null && context.TryGetValue(ScenarioContextTokenKey, out var token))
        {
            return token as string ?? string.Empty;
        }

        return string.Empty;
    }

    public static void ApplyToCurrentScenario(string token) =>
        ScenarioToken.Value = token;

    public static void InitializeFromConfig(string appSettingsFile = "appsettings.json")
    {
        var cached = ConfigReaderNew.GetJsonValue(appSettingsFile, "access_token");
        if (!string.IsNullOrWhiteSpace(cached))
        {
            ScenarioToken.Value = cached;
        }
    }

    public static void SetAccessToken(
        string token,
        string appSettingsFile = "appsettings.json",
        bool persistToConfig = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ScenarioToken.Value = token;
        BoundScenario.Value?.Set(token, ScenarioContextTokenKey);

        if (persistToConfig && AppConfiguration.Authentication.PersistAccessToken)
        {
            ConfigReaderNew.UpdateJsonValue(appSettingsFile, "access_token", token);
        }
    }

    public static void ClearScenarioToken()
    {
        ScenarioToken.Value = null;
        BoundScenario.Value?.Set(string.Empty, ScenarioContextTokenKey);
    }

    public static void ResetForNewScenario()
    {
        var settings = AppConfiguration.Authentication;

        if (settings.Mode == AuthenticationMode.PerScenario)
        {
            ScenarioToken.Value = null;
            if (settings.UseCachedAccessToken)
            {
                InitializeFromConfig();
            }

            return;
        }

        SharedTokenProvider.ApplySharedTokenToScenario();
    }
}
