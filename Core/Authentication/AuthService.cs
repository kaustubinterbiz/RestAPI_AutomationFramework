using System.Globalization;
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

    /// <param name="forceRefresh">True for explicit login steps; false when reusing a shared token.</param>
    public static Task<RestResponse> LoginAndStoreTokenAsync(ApiClient apiClient, bool forceRefresh = true) =>
        SharedTokenProvider.LoginAndStoreTokenAsync(apiClient, FetchTokenFromApiAsync, forceRefresh);

    public static Task<RestResponse> LoginAndStoreTokenAsync(ApiClient apiClient, string? roleType= "AdminRole", bool forceRefresh = true) =>
        SharedTokenProvider.LoginAndStoreTokenAsync(apiClient, client => FetchTokenFromCredentialRoleType_ApiAsync(client, roleType), forceRefresh);
   
    public static Task EnsureAuthenticatedAsync(ApiClient apiClient) =>
        SharedTokenProvider.EnsureAuthenticatedAsync(apiClient, FetchTokenFromApiAsync);

    /// <summary>POST token endpoint with an existing bearer token (re-login / logout validation).</summary>
    public static async Task<RestResponse> LoginWithBearerTokenAsync(ApiClient apiClient, string bearerToken)
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
            ?? throw new JsonException($"Failed to deserialize login credentials for '{loginRoleKey}'.");

        ConfigReaderNew.LoadConfig(endpointJsonPath);
        var loginEndpoint = ConfigReaderNew.GetValue("post");

        return await apiClient.LoginPostAsync(loginEndpoint, credentials, bearerToken);
    }

    /// <summary>POST token endpoint with bearer only (simulates reuse/expired token without new ROPC login).</summary>
    public static async Task<RestResponse> LoginWithBearerOnlyAsync(ApiClient apiClient, string bearerToken)
    {
        ConfigReaderNew.LoadConfig(AppSettingsFile);
        ConfigReaderNew.LoadConfig(ConfigReaderNew.GetValue("EndpointJson"));
        var loginEndpoint = ConfigReaderNew.GetValue("post");
        return await apiClient.LoginPostBearerOnlyAsync(loginEndpoint, bearerToken);
    }

    private static async Task<(string? Token, DateTimeOffset ExpiresAtUtc, RestResponse Response)> FetchTokenFromApiAsync(ApiClient apiClient)
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
            return (null, default, response);
        }

        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(response.Content, JsonOptions);
        var token = loginResponse?.access_token;

        if (string.IsNullOrWhiteSpace(token))
        {
            return (null, default, response);
        }

        int? expiresInSeconds = null;
        if (!string.IsNullOrWhiteSpace(loginResponse?.expires_in)
            && int.TryParse(loginResponse.expires_in, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            expiresInSeconds = seconds;
        }

        var expiresAtUtc = JwtTokenHelper.ResolveExpiry(token, expiresInSeconds);
        return (token, expiresAtUtc, response);
    }

    private static async Task<(string? Token, DateTimeOffset ExpiresAtUtc, RestResponse Response)> 
        FetchTokenFromCredentialRoleType_ApiAsync(ApiClient apiClient, string roleType)
    {
        ConfigReaderNew.LoadConfig(AppSettingsFile);

        var loginJsonPath = ConfigReaderNew.GetValue("LoginJson");
        var endpointJsonPath = ConfigReaderNew.GetValue("EndpointJson");

        var credentialsJson = ConfigReaderNew.GetJsonBody(loginJsonPath, roleType);
        var credentials = JsonSerializer.Deserialize<LoginRequest>(credentialsJson, JsonOptions)
            ?? throw new JsonException(
                $"Failed to deserialize login credentials for key '{roleType}' in '{loginJsonPath}'.");

        ConfigReaderNew.LoadConfig(endpointJsonPath);
        var loginEndpoint = ConfigReaderNew.GetValue("post");

        var response = await apiClient.LoginPostAsync(loginEndpoint, credentials);

        if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
        {
            return (null, default, response);
        }

        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(response.Content, JsonOptions);
        var token = loginResponse?.access_token;

        if (string.IsNullOrWhiteSpace(token))
        {
            return (null, default, response);
        }

        int? expiresInSeconds = null;
        if (!string.IsNullOrWhiteSpace(loginResponse?.expires_in)
            && int.TryParse(loginResponse.expires_in, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            expiresInSeconds = seconds;
        }

        var expiresAtUtc = JwtTokenHelper.ResolveExpiry(token, expiresInSeconds);
        return (token, expiresAtUtc, response);
    }
}
