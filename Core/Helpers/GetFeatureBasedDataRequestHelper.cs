using EnterpriseApiAutomationFramework.Core.Builders;
using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Core.Security.Patient;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Resolves GetFeatureBasedData POST request values to match the APIM curl contract:
/// POST api/v2/Patient/{patientId}/0
/// </summary>
public static class GetFeatureBasedDataRequestHelper
{
    public const string BodySheetKey = "patientGetFeatureBasedData_Body";
    public const string EndpointKey = "patientGetFeatureBasedData";
    public const string DefaultDataType = "0";
    private const string BusinessUnitPlaceholder = "{{BusinessUnitId}}";

    public static string GetFeatureBasedDataBodyTemplate =>
        """
        {"PatientDemographic":{"Filter":{"BusinessUnitId":"{{BusinessUnitId}}","Limit":10}},"AllergyIntolerance":{"FilterModel":{"BusinessUnitId":"{{BusinessUnitId}}","Limit":10}},"Condition":{"Filter":{"BusinessUnitId":"{{BusinessUnitId}}","Limit":10}},"Coverage":{"Filter":{"BusinessUnitId":"{{BusinessUnitId}}","Limit":10}},"Immunization":{"Filter":{"BusinessUnitId":"{{BusinessUnitId}}","Limit":10}},"MedicationRequest":{"Filter":{"BusinessUnitId":"{{BusinessUnitId}}","Limit":10}},"ProgressNote":{"Filter":{"BusinessUnitId":"{{BusinessUnitId}}","Limit":10,"ShowAllProgressNotes":false},"Output":{"ProgressNote":true}}}
        """;

    public static string ResolvePatientId(string configFile = "appsettings.json")
    {
        ConfigReaderNew.LoadConfig(configFile);

        var fromTestData = ConfigReaderNew.GetValue("PatientSecurityTestData:FeatureBasedPatientId");
        if (!string.IsNullOrWhiteSpace(fromTestData))
            return fromTestData;

        var fromOwned = ConfigReaderNew.GetValue("A_OWNED_PATIENT_ID");
        if (!string.IsNullOrWhiteSpace(fromOwned)
            && !string.Equals(fromOwned, "00000000-0000-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
        {
            return fromOwned;
        }

        var fromOrgA = PatientSecurityTestDataConfig.OrgAPatientId;
        if (!string.IsNullOrWhiteSpace(fromOrgA)
            && !string.Equals(fromOrgA, "00000000-0000-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
        {
            return fromOrgA;
        }

        return "129a6ca6-9542-4488-bcc7-38ada94f4389";
    }

    public static string ResolveDataType(string configFile = "appsettings.json")
    {
        ConfigReaderNew.LoadConfig(configFile);

        var fromTestData = ConfigReaderNew.GetValue("PatientSecurityTestData:FeatureBasedDataType");
        if (!string.IsNullOrWhiteSpace(fromTestData))
            return fromTestData;

        return DefaultDataType;
    }

    public static Dictionary<string, string> ResolveGetFeatureBasedDataUrlSegments(
        string configFile = "appsettings.json",
        string? dataTypeOverride = null) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["patientId"] = ResolvePatientId(configFile),
            ["dataType"] = string.IsNullOrWhiteSpace(dataTypeOverride)
                ? ResolveDataType(configFile)
                : dataTypeOverride.Trim()
        };

    /// <summary>
    /// Hospital-owned patient feature data is scoped to org-A / BusinessUnitInfo (curl contract).
    /// </summary>
    public static string ResolveFeatureBasedBusinessUnitId(string configFile = "appsettings.json")
    {
        ConfigReaderNew.LoadConfig(configFile);

        var fromOrgA = ConfigReaderNew.GetValue("PatientSecurityTestData:OrgABusinessUnitId");
        if (!string.IsNullOrWhiteSpace(fromOrgA))
            return fromOrgA;

        return PatientListRequestHelper.ResolveBusinessUnitId(configFile);
    }

    public static string ResolveGetFeatureBasedDataBodyJson(string configFile = "appsettings.json")
    {
        var raw = RequestBuilder.ResolveBodyFromConfig(BodySheetKey, configFile)
            ?? GetFeatureBasedDataBodyTemplate;

        var businessUnitId = ResolveFeatureBasedBusinessUnitId(configFile);

        return raw.Replace(BusinessUnitPlaceholder, businessUnitId, StringComparison.Ordinal);
    }
}
