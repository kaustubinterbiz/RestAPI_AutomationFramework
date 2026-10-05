using EnterpriseApiAutomationFramework.Core.Builders;
using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Resolves Patient List POST request values to match the browser/APIM curl contract.
/// </summary>
public static class PatientListRequestHelper
{
    public const string BodySheetKey = "patientList_Body";
    private const string BusinessUnitPlaceholder = "{{BusinessUnitId}}";
    private const string LastDurationPlaceholder = "{{LastDurationOfRecords}}";

    public static string PatientListBodyTemplate =>
        $$"""
          {"BusinessUnitIds":["{{BusinessUnitPlaceholder}}"],"PatientVisibilityInDays":30,"Status":"All","Mode":"Recent","Insurance":"All","PatientName":"","ServiceRequestedBy":"All","Offset":0,"Limit":25,"TrackedBy":"All","TagNames":[],"LastDurationOfRecords":"{{LastDurationPlaceholder}}"}
          """;

    public static string ResolveBusinessUnitId(string configFile = "appsettings.json")
    {
        ConfigReaderNew.LoadConfig(configFile);

        var fromBusinessUnitInfo = ConfigReaderNew.GetValue("BusinessUnitInfo:BusinessUnitId");
        if (!string.IsNullOrWhiteSpace(fromBusinessUnitInfo))
            return fromBusinessUnitInfo;

        try
        {
            var cached = EndpointRequestHelper.GetCachedValue("BusinessUnitId");
            if (!string.IsNullOrWhiteSpace(cached))
                return cached;
        }
        catch (InvalidOperationException)
        {
            // Fall through to root appsettings lookup.
        }

        var fromRoot = ConfigReaderNew.GetValue("BusinessUnitId");
        if (!string.IsNullOrWhiteSpace(fromRoot))
            return fromRoot;

        throw new InvalidOperationException(
            "BusinessUnitId is not available for Patient List. Run session step after login or set BusinessUnitInfo:BusinessUnitId in appsettings.");
    }

    public static string ResolveLastDurationOfRecords() =>
        DateTime.UtcNow.AddDays(-30).ToString("O");

    public static Dictionary<string, string> ResolvePatientListUrlSegments(string configFile = "appsettings.json") =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["businessunitId"] = ResolveBusinessUnitId(configFile)
        };

    public static string ResolvePatientListBodyJson(string configFile = "appsettings.json")
    {
        var raw = RequestBuilder.ResolveBodyFromConfig(BodySheetKey, configFile)
            ?? PatientListBodyTemplate;

        var businessUnitId = ResolveBusinessUnitId(configFile);
        var lastDuration = ResolveLastDurationOfRecords();

        return raw
            .Replace(BusinessUnitPlaceholder, businessUnitId, StringComparison.Ordinal)
            .Replace(LastDurationPlaceholder, lastDuration, StringComparison.Ordinal);
    }
}
