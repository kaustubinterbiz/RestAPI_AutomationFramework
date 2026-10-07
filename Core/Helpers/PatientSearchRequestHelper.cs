using System.Text.Json;
using System.Text.Json.Nodes;
using EnterpriseApiAutomationFramework.Core.Builders;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Resolves Patient Search POST request values to match the APIM curl contract:
/// POST api/v2/Patient/Search/1
/// Sort maps to Mode; By maps to OrderBy (Patient List contract).
/// </summary>
public static class PatientSearchRequestHelper
{
    public const string EndpointKey = "patientSearch";
    public const string BodySheetKey = "patientSearch_Body";
    public const string FilterSheetKey = "patientSearch_Filter";

    private const string FirstNamePlaceholder = "{{FirstName}}";
    private const string LastNamePlaceholder = "{{LastName}}";
    private const string DobPlaceholder = "{{Dob}}";
    private const string InsurancePlaceholder = "{{Insurance}}";
    private const string SortPlaceholder = "{{Sort}}";
    private const string ByPlaceholder = "{{By}}";

    public static string PatientSearchBodyTemplate =>
        """
        {"Insurance":"{{Insurance}}","Mode":"{{Sort}}","OrderBy":"{{By}}","PatientDemographic":{"Filter":{"Ssn":[null],"Dob":["{{Dob}}"],"FirstName":["{{FirstName}}"],"LastName":["{{LastName}}"],"Gender":[null],"Zipcode":[],"StreetAddress":[],"City":[],"Email":[],"Phone":[],"InsuranceId":[],"InsuranceName":[]}}}
        """;

    public static IReadOnlyDictionary<string, string> DefaultFilterValues { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["FirstName"] = "First Fax",
            ["LastName"] = "Iverson",
            ["Dob"] = "01-01-1960"
        };

    public static string ResolvePatientSearchBodyJson(string configFile = "appsettings.json") =>
        ResolvePatientSearchBodyJson("All", "Recent", "-", configFile);

    public static string ResolvePatientSearchBodyJson(
        string insurance,
        string sort,
        string? by,
        string configFile = "appsettings.json")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(insurance);
        ArgumentException.ThrowIfNullOrWhiteSpace(sort);

        var normalizedSort = sort.Trim();
        var normalizedBy = NormalizeBy(by);
        ValidateSortByCombination(normalizedSort, normalizedBy);

        var raw = RequestBuilder.ResolveBodyFromConfig(BodySheetKey, configFile)
            ?? PatientSearchBodyTemplate;

        raw = raw
            .Replace(FirstNamePlaceholder, ResolveFilterValue("FirstName"), StringComparison.Ordinal)
            .Replace(LastNamePlaceholder, ResolveFilterValue("LastName"), StringComparison.Ordinal)
            .Replace(DobPlaceholder, ResolveFilterValue("Dob"), StringComparison.Ordinal)
            .Replace(InsurancePlaceholder, insurance.Trim(), StringComparison.Ordinal)
            .Replace(SortPlaceholder, normalizedSort, StringComparison.Ordinal);

        if (IsRecentSort(normalizedSort) || normalizedBy == null)
        {
            raw = raw.Replace(",\"OrderBy\":\"{{By}}\"", string.Empty, StringComparison.Ordinal)
                .Replace("\"OrderBy\":\"{{By}}\",", string.Empty, StringComparison.Ordinal)
                .Replace("\"OrderBy\":\"{{By}}\"", string.Empty, StringComparison.Ordinal);
        }
        else
        {
            raw = raw.Replace(ByPlaceholder, normalizedBy, StringComparison.Ordinal);
        }

        var node = JsonNode.Parse(raw)?.AsObject()
            ?? throw new InvalidOperationException("Patient Search body template is not valid JSON.");

        if (IsRecentSort(normalizedSort) || normalizedBy == null)
            node.Remove("OrderBy");

        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static string? NormalizeBy(string? by)
    {
        if (string.IsNullOrWhiteSpace(by) || by.Trim() == "-")
            return null;

        return by.Trim();
    }

    private static bool IsRecentSort(string sort) =>
        string.Equals(sort, "Recent", StringComparison.OrdinalIgnoreCase);

    private static void ValidateSortByCombination(string sort, string? by)
    {
        if (IsRecentSort(sort))
            return;

        if (by == null)
        {
            throw new InvalidOperationException(
                $"Patient Search Sort '{sort}' requires a By value (A-Z, Z-A, Latest, or Oldest).");
        }

        var nameOrInsurance = string.Equals(sort, "Name", StringComparison.OrdinalIgnoreCase)
            || string.Equals(sort, "Insurance", StringComparison.OrdinalIgnoreCase);
        var admittedOrDischarge = string.Equals(sort, "Admitted", StringComparison.OrdinalIgnoreCase)
            || string.Equals(sort, "Discharge", StringComparison.OrdinalIgnoreCase);

        if (nameOrInsurance
            && !string.Equals(by, "A-Z", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(by, "Z-A", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Sort '{sort}' only supports By values A-Z or Z-A (got '{by}').");
        }

        if (admittedOrDischarge
            && !string.Equals(by, "Latest", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(by, "Oldest", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Sort '{sort}' only supports By values Latest or Oldest (got '{by}').");
        }
    }

    private static string ResolveFilterValue(string key)
    {
        if (ExcelConfigReader.TryGetKeyValue(
                TestConfigDefaults.BodyExcelFile,
                FilterSheetKey,
                key,
                out var value)
            && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (DefaultFilterValues.TryGetValue(key, out var fallback))
            return fallback;

        throw new InvalidOperationException(
            $"Missing '{key}' in '{TestConfigDefaults.BodyExcelFile}' sheet '{FilterSheetKey}'.");
    }
}
