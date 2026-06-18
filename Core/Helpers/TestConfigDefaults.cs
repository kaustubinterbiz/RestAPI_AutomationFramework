namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Excel workbook and sheet names for config migrated from JSON (Phase 1+).
/// appsettings.json paths stay unchanged; readers use these Excel targets.
/// </summary>
public static class TestConfigDefaults
{
    public const string EndpointExcelFile = "RequestEndPoint.xlsx";
    public const string EndpointSheet = "Endpoints";
    public const string EndpointResponseSheet = "Endpoint_Response";

    public const string KeyColumn = "Key";
    public const string ValueColumn = "Value";
    public const string HttpStatusColumn = "HttpStatus";
    public const string ResponseSnippetColumn = "ResponseSnippet";
    public const string UpdatedAtColumn = "UpdatedAt";
}
