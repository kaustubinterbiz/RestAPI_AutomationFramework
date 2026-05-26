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

    public async Task<RestResponse> GetFromConfig(
        string env = "appsettings.json",
        string endpointJsonKey = "EndpointJson",
        string endpointKey = "get",
        ApiHost? host = null,
        bool ensureAuth = true,
        ApiGetRequestOptions? getOptions = null)
    {
        if (ensureAuth && (getOptions is null || !getOptions.BearerTokenProvided))
        {
            await AuthService.EnsureAuthenticatedAsync(_apiClient);
        }

        ConfigReaderNew.LoadConfig(env);
        ConfigReaderNew.LoadConfig(ConfigReaderNew.GetValue(endpointJsonKey));
        var endpoint = ConfigReaderNew.GetValue(endpointKey);
        return await _apiClient.GetAsync(endpoint, getOptions, host ?? ApiHostContext.CurrentOrDefault);
    }

    public Task<RestResponse> GetUsers(string env, string key, string request, ApiHost? host = null) =>
        GetFromConfig(env, key, request, host);

    public Task<RestResponse> GetUsersWithCurrentTokenOnly(
        string env,
        string key,
        string request,
        ApiHost? host = null) =>
        GetFromConfig(env, key, request, host, ensureAuth: false);

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
