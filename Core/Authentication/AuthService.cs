using System.Text.Json;
using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Models.Request;
using EnterpriseApiAutomationFramework.Models.Response;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Authentication;

public static class AuthService
{
    private const string AppSettingsFile = "appsettings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static void LoadTokenFromConfig() => TokenManager.InitializeFromConfig();

    /// <summary>
    /// POST token endpoint on Auth host, store bearer token for Api host calls.
    /// </summary>
    public static async Task<RestResponse> LoginAndStoreTokenAsync(ApiClient apiClient)
    {
        ConfigReaderNew.LoadConfig(AppSettingsFile);

        var loginJsonPath = ConfigReaderNew.GetValue("LoginJson");
        var endpointJsonPath = ConfigReaderNew.GetValue("EndpointJson");

        var loginRoleKey = ConfigReaderNew.GetValue("LoginRoleKey");
        if (string.IsNullOrWhiteSpace(loginRoleKey))
        {
            loginRoleKey = "OrganizationRole";
        }

        var credentialsJson = ConfigReaderNew.GetJsonBody(loginJsonPath, loginRoleKey);
        var credentials = JsonSerializer.Deserialize<LoginRequest>(credentialsJson, JsonOptions)
            ?? throw new JsonException(
                $"Failed to deserialize login credentials for key '{loginRoleKey}' in '{loginJsonPath}'.");

        ConfigReaderNew.LoadConfig(endpointJsonPath);
        var loginEndpoint = ConfigReaderNew.GetValue("post");

        var response = await apiClient.LoginPostAsync(loginEndpoint, credentials);

        if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
        {
            return response;
        }

        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(response.Content, JsonOptions);
        var token = loginResponse?.access_token;

        if (!string.IsNullOrWhiteSpace(token))
        {
            TokenManager.SetAccessToken(token, AppSettingsFile);
        }

        return response;
    }

    /// <summary>Login only when the current scenario has no token yet.</summary>
    public static async Task EnsureAuthenticatedAsync(ApiClient apiClient)
    {
        if (TokenManager.HasToken)
        {
            return;
        }

        var response = await LoginAndStoreTokenAsync(apiClient);
        if (!response.IsSuccessful || !TokenManager.HasToken)
        {
            throw new InvalidOperationException(
                $"Authentication failed. Status={(int)response.StatusCode}, Body={response.Content}");
        }
    }
}
