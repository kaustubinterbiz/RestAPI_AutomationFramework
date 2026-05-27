using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Configurations;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.Core.Authentication;

/// <summary>
/// Single place for token handling. Steps and drivers should use this class only.
/// Flow: scenario token → appsettings.json "access_token" → login API.
/// </summary>
public static class ApiAuth
{
    public static void LoadTokenFromAppSettings(string appSettingsFile = "appsettings.json")
    {
        ConfigReaderNew.LoadConfig(appSettingsFile);
        TokenManager.InitializeFromConfig(appSettingsFile);
    }

    /// <summary>
    /// Makes sure a Bearer token exists before an API call (GET/POST/PUT...).
    /// </summary>
    public static async Task EnsureReadyAsync(ApiClient apiClient, string appSettingsFile = "appsettings.json")
    {
        LoadTokenFromAppSettings(appSettingsFile);

        if (TokenManager.HasToken)
        {
            return;
        }

        await AuthService.EnsureAuthenticatedAsync(apiClient);
        LoadTokenFromAppSettings(appSettingsFile);

        if (!TokenManager.HasToken)
        {
            throw new InvalidOperationException(
                "No access token available. Add a login step or set 'access_token' in appsettings.json.");
        }
    }

    public static void SaveTokenFromLoginResponse(ScenarioContext context, string? loginResponseBody)
    {
        var token = LoginResponseParser.TryGetAccessToken(loginResponseBody);
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Login response did not contain access_token.");
        }

        TokenContext.StoreAccessToken(context, token);
    }
}
