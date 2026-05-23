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

    /// <summary>Token via Auth host (B2C); token stored for Api host calls.</summary>
    public Task<RestResponse> LoginAsync() =>
        AuthService.LoginAndStoreTokenAsync(_apiClient);

    /// <summary>Api host GET with bearer token from login.</summary>
    public async Task<RestResponse> GetUsers(string env, string key, string request)
    {
        await AuthService.EnsureAuthenticatedAsync(_apiClient);

        ConfigReaderNew.LoadConfig(env);
        var endpointConfigPath = ConfigReaderNew.GetValue(key);
        ConfigReaderNew.LoadConfig(endpointConfigPath);
        var endPoint = ConfigReaderNew.GetValue(request);
        return await _apiClient.GetAsync(endPoint);
    }

    public async Task<RestResponse> PostUser(
        string env,
        string key,
        string request,
        string jsonBodyFileKey,
        string bodyKey)
    {
        await AuthService.EnsureAuthenticatedAsync(_apiClient);

        ConfigReaderNew.LoadConfig(env);
        var endpointConfigPath = ConfigReaderNew.GetValue(key);
        var requestBodyPath = ConfigReaderNew.GetValue(jsonBodyFileKey);
        ConfigReaderNew.LoadConfig(endpointConfigPath);
        var endPoint = ConfigReaderNew.GetValue(request);
        var json = ConfigReaderNew.GetJsonBody(requestBodyPath, bodyKey);
        return await _apiClient.PostAsync(endPoint, json);
    }

    public async Task<RestResponse> UpdateUser(object body)
    {
        await AuthService.EnsureAuthenticatedAsync(_apiClient);
        return await _apiClient.PutAsync("/users/2", body);
    }

    public async Task<RestResponse> PatchUser(object body)
    {
        await AuthService.EnsureAuthenticatedAsync(_apiClient);
        return await _apiClient.PatchAsync("/users/2", body);
    }

    public async Task<RestResponse> DeleteUser()
    {
        await AuthService.EnsureAuthenticatedAsync(_apiClient);
        return await _apiClient.DeleteAsync("/users/2");
    }
}
