using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Configurations;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Drivers;

public class UserDriver
{
    private readonly ApiClient _apiClient;

    public UserDriver(ApiClient? apiClient = null)
    {
        _apiClient = apiClient ?? new ApiClient();
    }

    /// <summary>Always calls B2C login API (used by login scenarios).</summary>
    public Task<RestResponse> LoginAsync() =>
        AuthService.LoginAndStoreTokenAsync(_apiClient, forceRefresh: true);

    public Task<RestResponse> RefreshAccessTokenAsync() =>
        AuthService.LoginAndStoreTokenAsync(_apiClient, forceRefresh: true);

    public Task<RestResponse> LoginWithStoredBearerTokenAsync(string bearerToken) =>
        AuthService.LoginWithBearerOnlyAsync(_apiClient, bearerToken);

    public void ApplyExpiredAccessToken(string? validToken = null) =>
        SharedTokenProvider.ApplyExpiredTokenForTesting(
            TokenTestHelper.GetExpiredAccessToken(
                validToken ?? TokenManager.AccessToken));

    /// <summary>GET with bearer token; uses <see cref="ApiHostContext"/> for base URL.</summary>
    public async Task<RestResponse> GetUsers(
        string env,
        string key,
        string request,
        ApiHost? host = null)
    {
        await AuthService.EnsureAuthenticatedAsync(_apiClient);

        return await GetUsersWithCurrentTokenOnly(env, key, request, host);
    }

    /// <summary>GET using the current scenario token only (no auto-login).</summary>
    public async Task<RestResponse> GetUsersWithCurrentTokenOnly(
        string env,
        string key,
        string request,
        ApiHost? host = null)
    {
        ConfigReaderNew.LoadConfig(env);
        var endpointConfigPath = ConfigReaderNew.GetValue(key);
        ConfigReaderNew.LoadConfig(endpointConfigPath);
        var endPoint = ConfigReaderNew.GetValue(request);
        return await _apiClient.GetAsync(endPoint, host ?? ApiHostContext.CurrentOrDefault);
    }

    public async Task<RestResponse> PostUser(
        string env,
        string key,
        string request,
        string jsonBodyFileKey,
        string bodyKey,
        ApiHost? host = null)
    {
        await AuthService.EnsureAuthenticatedAsync(_apiClient);

        ConfigReaderNew.LoadConfig(env);
        var endpointConfigPath = ConfigReaderNew.GetValue(key);
        var requestBodyPath = ConfigReaderNew.GetValue(jsonBodyFileKey);
        ConfigReaderNew.LoadConfig(endpointConfigPath);
        var endPoint = ConfigReaderNew.GetValue(request);
        var json = ConfigReaderNew.GetJsonBody(requestBodyPath, bodyKey);
        return await _apiClient.PostAsync(endPoint, json, host: host ?? ApiHostContext.CurrentOrDefault);
    }

    public async Task<RestResponse> UpdateUser(object body, ApiHost? host = null)
    {
        await AuthService.EnsureAuthenticatedAsync(_apiClient);
        return await _apiClient.PutAsync("/users/2", body, host: host ?? ApiHostContext.CurrentOrDefault);
    }

    public async Task<RestResponse> PatchUser(object body, ApiHost? host = null)
    {
        await AuthService.EnsureAuthenticatedAsync(_apiClient);
        return await _apiClient.PatchAsync("/users/2", body, host: host ?? ApiHostContext.CurrentOrDefault);
    }

    public async Task<RestResponse> DeleteUser(ApiHost? host = null)
    {
        await AuthService.EnsureAuthenticatedAsync(_apiClient);
        return await _apiClient.DeleteAsync("/users/2", host: host ?? ApiHostContext.CurrentOrDefault);
    }
}
