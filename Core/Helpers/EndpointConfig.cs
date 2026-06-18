namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Reads API paths from RequestEndPoint.xlsx (Endpoints sheet), with JSON fallback.
/// </summary>
public static class EndpointConfig
{
    public static string GetEndpoint(string endpointKey) =>
        ExcelConfigReader.GetEndpoint(endpointKey);
}
