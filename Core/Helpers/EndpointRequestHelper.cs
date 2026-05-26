using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Loads endpoint paths from config and resolves cached placeholders ({id}, {access_token}).
/// </summary>
public static class EndpointRequestHelper
{
    public static string LoadAndResolveEndpoint(
        string envFile = "appsettings.json",
        string endpointJsonKey = "EndpointJson",
        string endpointKey = "get")
    {
        ConfigReaderNew.LoadConfig(envFile);
        ConfigReaderNew.LoadConfig(ConfigReaderNew.GetValue(endpointJsonKey));
        var endpoint = ConfigReaderNew.GetValue(endpointKey);
        return EndpointHelper.ResolveEndpoint(endpoint).Endpoint;
    }

    public static string GetCachedValue(string key)
    {
        var value = key.Equals("access_token", StringComparison.OrdinalIgnoreCase)
            ? TokenManager.AccessToken
            : ConfigReaderNew.GetValue(key);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Cached config value '{key}' is missing. Check RequestEndPoint.json or login for access_token.");
        }

        return value;
    }
}
