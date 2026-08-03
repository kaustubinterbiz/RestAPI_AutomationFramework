using System.Globalization;
using System.Text.Json;
using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Core.Helpers;
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

    public static Task<RestResponse> LoginAndStoreTokenAsync(ApiClient apiClient, string roleType = "AdminRole", bool forceRefresh = true) =>
        SharedTokenProvider.LoginAndStoreTokenAsync(apiClient, client => FetchTokenFromCredentialRoleType_ApiAsync(client, roleType), forceRefresh);

    public static Task EnsureAuthenticatedAsync(ApiClient apiClient) =>
        SharedTokenProvider.EnsureAuthenticatedAsync(apiClient, FetchTokenFromApiAsync);

    /// <summary>POST token endpoint with an existing bearer token (re-login / logout validation).</summary>
    public static async Task<RestResponse> LoginWithBearerTokenAsync(ApiClient apiClient, string bearerToken)
    {
        ConfigReaderNew.LoadConfig(AppSettingsFile);

        var loginRoleKey = ConfigReaderNew.GetValue("LoginRoleKey");
        if (string.IsNullOrWhiteSpace(loginRoleKey))
            loginRoleKey = "OrganizationRole";

        var credentials = DeserializeLoginCredentials(loginRoleKey);
        var loginEndpoint = ExcelConfigReader.GetEndpoint("post");
        var response = await apiClient.LoginPostAsync(loginEndpoint, credentials, bearerToken);
        SaveLoginResponse(loginRoleKey, response, null);

        return response;
    }

    /// <summary>POST token endpoint with bearer only (simulates reuse/expired token without new ROPC login).</summary>
    public static async Task<RestResponse> LoginWithBearerOnlyAsync(ApiClient apiClient, string bearerToken)
    {
        ConfigReaderNew.LoadConfig(AppSettingsFile);
        var loginEndpoint = ExcelConfigReader.GetEndpoint("post");
        return await apiClient.LoginPostBearerOnlyAsync(loginEndpoint, bearerToken);
    }

    private static async Task<(string? Token, DateTimeOffset ExpiresAtUtc, RestResponse Response)> FetchTokenFromApiAsync(ApiClient apiClient)
    {
        ConfigReaderNew.LoadConfig(AppSettingsFile);

        var loginRoleKey = ConfigReaderNew.GetValue("LoginRoleKey");
        if (string.IsNullOrWhiteSpace(loginRoleKey))
            loginRoleKey = "OrganizationRole";

        var credentials = DeserializeLoginCredentials(loginRoleKey);
        var loginEndpoint = ExcelConfigReader.GetEndpoint("post");
        var response = await apiClient.LoginPostAsync(loginEndpoint, credentials);

        if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
        {
            SaveLoginResponse(loginRoleKey, response, null);
            return (null, default, response);
        }

        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(response.Content, JsonOptions);
        var token = loginResponse?.access_token;

        if (string.IsNullOrWhiteSpace(token))
        {
            SaveLoginResponse(loginRoleKey, response, null);
            return (null, default, response);
        }

        SaveLoginResponse(loginRoleKey, response, token);

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

        var credentials = DeserializeLoginCredentials(roleType);
        var loginEndpoint = ExcelConfigReader.GetEndpoint("post");
        var response = await apiClient.LoginPostAsync(loginEndpoint, credentials);

        if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
        {
            SaveLoginResponse(roleType, response, null);
            return (null, default, response);
        }

        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(response.Content, JsonOptions);
        var token = loginResponse?.access_token;

        if (string.IsNullOrWhiteSpace(token))
        {
            SaveLoginResponse(roleType, response, null);
            return (null, default, response);
        }

        SaveLoginResponse(roleType, response, token);

        int? expiresInSeconds = null;
        if (!string.IsNullOrWhiteSpace(loginResponse?.expires_in)
            && int.TryParse(loginResponse.expires_in, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            expiresInSeconds = seconds;
        }

        var expiresAtUtc = JwtTokenHelper.ResolveExpiry(token, expiresInSeconds);
        return (token, expiresAtUtc, response);
    }

    private static LoginRequest DeserializeLoginCredentials(string role)
    {
        var credentialsJson = ExcelConfigReader.GetLoginCredentialsJson(role);
        return JsonSerializer.Deserialize<LoginRequest>(credentialsJson, JsonOptions)
            ?? throw new JsonException($"Failed to deserialize login credentials for '{role}'.");
    }

    private static void SaveLoginResponse(string role, RestResponse response, string? token)
    {
        var status = ((int)response.StatusCode).ToString();
        var snippet = string.IsNullOrWhiteSpace(token)
            ? Truncate(response.Content, 120)
            : Truncate(token, 40);

        ExcelConfigWriter.UpsertLoginResponse(role, status, snippet);

        try
        {
            ExcelConfigWriter.UpsertApiResponse(
                TestConfigDefaults.LoginApiKey,
                response.Content ?? string.Empty,
                httpStatus: status,
                primaryValue: role);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or TimeoutException)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Excel login API response upsert skipped: {ex.Message}");
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
