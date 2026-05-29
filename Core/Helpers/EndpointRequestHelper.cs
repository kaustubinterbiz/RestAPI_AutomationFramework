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
        if (key.Equals("access_token", StringComparison.OrdinalIgnoreCase))
        {
            return RequireValue(key, TokenManager.AccessToken);
        }

        var fromAppSettings = SessionInfoStore.GetCachedValue(key);
        if (!string.IsNullOrWhiteSpace(fromAppSettings))
        {
            return fromAppSettings;
        }

        return RequireValue(key, ConfigReaderNew.GetValue(key));
    }

    private static string RequireValue(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Cached config value '{key}' is missing. Run GetSession step or check appsettings.json / RequestEndPoint.json.");
        }

        return value;
    }
}
