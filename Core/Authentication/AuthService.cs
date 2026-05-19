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

    public static void LoadTokenFromConfig()
    {
        TokenManager.InitializeFromConfig();
    }

    public static async Task<RestResponse> LoginAndStoreTokenAsync(ApiClient apiClient)
    {
        ConfigReaderNew.LoadConfig(AppSettingsFile);

        var loginJsonPath = ConfigReaderNew.GetValue("LoginJson");
        var credentials = ConfigReaderNew.ReadJson<LoginRequest>(loginJsonPath);

        var endpointJsonPath = ConfigReaderNew.GetValue("EndpointJson");
        ConfigReaderNew.LoadConfig(endpointJsonPath);
        var loginEndpoint = ConfigReaderNew.GetValue("post");

        var response = await apiClient.PostAsync(loginEndpoint, credentials, authorizationRequired: false);

        if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
        {
            return response;
        }

        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(response.Content, JsonOptions);
        var token = loginResponse?.token;

        if (!string.IsNullOrWhiteSpace(token))
        {
            TokenManager.SetAccessToken(token, AppSettingsFile);
        }

        return response;
    }
}
