using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Reads API paths from TestData/Request Endpoint/RequestEndPoint.json.
/// </summary>
public static class EndpointConfig
{
    private const string AppSettingsFile = "appsettings.json";
    private const string EndpointJsonKey = "EndpointJson";

    public static string GetEndpoint(string endpointKey)
    {
        ConfigReaderNew.LoadConfig(AppSettingsFile);
        ConfigReaderNew.LoadConfig(ConfigReaderNew.GetValue(EndpointJsonKey));
        return ConfigReaderNew.GetValue(endpointKey);
    }
}
