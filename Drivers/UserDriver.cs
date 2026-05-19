using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Configurations;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Drivers;

public class UserDriver
{
    private readonly ApiClient _apiClient;
    
    public UserDriver()
    {
        _apiClient = new ApiClient();
    }

    public async Task<RestResponse> GetUsers(string env, string key, string request)
    {
        ConfigReaderNew.LoadConfig(env);
        string value = ConfigReaderNew.GetValue(key);
        ConfigReaderNew.LoadConfig(value);
        string endPoint = ConfigReaderNew.GetValue(request);
        return await _apiClient.GetAsync(endPoint);
    }

    public async Task<RestResponse> CreateUser(object body)
    {
        return await _apiClient.PostAsync("/api/Authenticate/Login", body);
    }

    public async Task<RestResponse> UpdateUser(object body)
    {
        return await _apiClient.PutAsync("/users/2", body);
    }

    public async Task<RestResponse> PatchUser(object body)
    {
        return await _apiClient.PatchAsync("/users/2", body);
    }

    public async Task<RestResponse> DeleteUser()
    {
        return await _apiClient.DeleteAsync("/users/2");
    }
}