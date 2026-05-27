using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Core.Helpers;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Drivers;

/// <summary>
/// All API actions for tests. Steps call this class — not ApiClient directly.
/// </summary>
public class UserDriver
{
    private readonly ApiClient _apiClient;

    public UserDriver(ApiClient? apiClient = null)
    {
        _apiClient = apiClient ?? new ApiClient();
    }

    /// <summary>Login on Auth (B2C) host and save token.</summary>
    public Task<RestResponse> LoginAsync() =>
        AuthService.LoginAndStoreTokenAsync(_apiClient, forceRefresh: true);

    public Task<RestResponse> RefreshAccessTokenAsync() =>
        LoginAsync();

    public Task<RestResponse> LoginWithStoredBearerTokenAsync(string bearerToken) =>
        AuthService.LoginWithBearerOnlyAsync(_apiClient, bearerToken);

    public void ApplyExpiredAccessToken(string? validToken = null) =>
        SharedTokenProvider.ApplyExpiredTokenForTesting(
            TokenTestHelper.GetExpiredAccessToken(validToken ?? TokenManager.AccessToken));

    /// <summary>
    /// Authenticated GET. Loads endpoint from RequestEndPoint.json and sends Authorization: Bearer token.
    /// </summary>
    public async Task<RestResponse> GetAsync(string endpointKey = "get", ApiHost? host = null)
    {
        await ApiAuth.EnsureReadyAsync(_apiClient);
        var endpoint = EndpointConfig.GetEndpoint(endpointKey);
        return await _apiClient.GetAsync(endpoint, host: host ?? ApiHostContext.CurrentOrDefault);
    }

    /// <summary>GET with existing token only — does not call login again (token refresh tests).</summary>
    public async Task<RestResponse> GetWithCurrentTokenAsync(string endpointKey = "get", ApiHost? host = null)
    {
        ApiAuth.LoadTokenFromAppSettings();
        var endpoint = EndpointConfig.GetEndpoint(endpointKey);
        return await _apiClient.GetAsync(endpoint, host: host ?? ApiHostContext.CurrentOrDefault);
    }

    public async Task<RestResponse> PostFromConfigAsync(
        string endpointKey,
        string bodyFileKey,
        string bodyKey,
        ApiHost? host = null)
    {
        await ApiAuth.EnsureReadyAsync(_apiClient);

        var endpoint = EndpointConfig.GetEndpoint(endpointKey);
        ConfigReaderNew.LoadConfig(AppSettingsFile);
        var bodyPath = ConfigReaderNew.GetValue(bodyFileKey);
        var json = ConfigReaderNew.GetJsonBody(bodyPath, bodyKey);

        return await _apiClient.PostAsync(endpoint, json, host: host ?? ApiHostContext.CurrentOrDefault);
    }

    public async Task<RestResponse> UpdateUser(object body, ApiHost? host = null)
    {
        await ApiAuth.EnsureReadyAsync(_apiClient);
        return await _apiClient.PutAsync("/users/2", body, host: host ?? ApiHostContext.CurrentOrDefault);
    }

    public async Task<RestResponse> PatchUser(object body, ApiHost? host = null)
    {
        await ApiAuth.EnsureReadyAsync(_apiClient);
        return await _apiClient.PatchAsync("/users/2", body, host: host ?? ApiHostContext.CurrentOrDefault);
    }

    public async Task<RestResponse> DeleteUser(ApiHost? host = null)
    {
        await ApiAuth.EnsureReadyAsync(_apiClient);
        return await _apiClient.DeleteAsync("/users/2", host: host ?? ApiHostContext.CurrentOrDefault);
    }

    private const string AppSettingsFile = "appsettings.json";

    // --- Legacy names (old steps still work) ---

    public Task<RestResponse> GetFromConfig(
        string env = AppSettingsFile,
        string endpointJsonKey = "EndpointJson",
        string endpointKey = "get",
        ApiHost? host = null,
        bool ensureAuth = true,
        ApiGetRequestOptions? getOptions = null) =>
        ensureAuth && (getOptions is null || !getOptions.BearerTokenProvided)
            ? GetAsync(endpointKey, host)
            : GetWithCurrentTokenAsync(endpointKey, host);

    public Task<RestResponse> GetUsers(string env, string key, string request, ApiHost? host = null) =>
        GetAsync(request, host);

    public Task<RestResponse> GetUsersWithCurrentTokenOnly(
        string env,
        string key,
        string request,
        ApiHost? host = null) =>
        GetWithCurrentTokenAsync(request, host);

    public Task<RestResponse> PostUser(
        string env,
        string key,
        string request,
        string jsonBodyFileKey,
        string bodyKey,
        ApiHost? host = null) =>
        PostFromConfigAsync(request, jsonBodyFileKey, bodyKey, host);

    [Obsolete("Use ApiAuth.LoadTokenFromAppSettings()")]
    public static void LoadAccessTokenFromAppSettings(string appSettingsFile = AppSettingsFile) =>
        ApiAuth.LoadTokenFromAppSettings(appSettingsFile);
}
