using EnterpriseApiAutomationFramework.Core.Builders;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Resolves GetFhirData POST request values to match the APIM curl contract.
/// </summary>
public static class GetFhirDataRequestHelper
{
    public const string BodySheetKey = "patientGetFhirData_Body";
    public const string EndpointKey = "patientGetFhirData";
    private const string BusinessUnitPlaceholder = "{{BusinessUnitId}}";

    public static string GetFhirDataBodyTemplate =>
        $$"""
          {"businessUnitIds":["{{BusinessUnitPlaceholder}}"],"currentBusinessUnitId":"{{BusinessUnitPlaceholder}}"}
          """;

    public static Dictionary<string, string> ResolveGetFhirDataUrlSegments(string configFile = "appsettings.json") =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["businessunitId"] = PatientListRequestHelper.ResolveBusinessUnitId(configFile)
        };

    public static string ResolveGetFhirDataBodyJson(string configFile = "appsettings.json")
    {
        var raw = RequestBuilder.ResolveBodyFromConfig(BodySheetKey, configFile)
            ?? GetFhirDataBodyTemplate;

        var businessUnitId = PatientListRequestHelper.ResolveBusinessUnitId(configFile);

        return raw.Replace(BusinessUnitPlaceholder, businessUnitId, StringComparison.Ordinal);
    }
}
