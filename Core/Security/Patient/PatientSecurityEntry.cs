using System.Text.RegularExpressions;

namespace EnterpriseApiAutomationFramework.Core.Security.Patient;

/// <summary>
/// One row from API_Security_Test_Matrix_P0.xlsx (Patient API groups only).
/// </summary>
public sealed class PatientSecurityEntry
{
    public required string TestCaseId { get; init; }
    public required string ApiId { get; init; }
    public required string Category { get; init; }
    public required string HttpMethod { get; init; }
    public required string EndpointPath { get; init; }
    public required string EndpointKey { get; init; }
    public required string ScenarioType { get; init; }
    public required string Role { get; init; }
    public required int ExpectedStatus { get; init; }
    public required IReadOnlyList<int> AllowedStatuses { get; init; }
    public string? MutationApplied { get; init; }
    public string? TamperedRequest { get; init; }
    public string? UrlSegmentKeys { get; init; }
    public string? QueryParamKeys { get; init; }
    public string? BodyKey { get; init; }
    public bool IsDestructive { get; init; }
    public bool SkipAutomation { get; init; }

    public string Label => $"{TestCaseId} | {ScenarioType} | {HttpMethod} {EndpointKey}";

    public string ExpectedStatusDisplay => string.Join("|", AllowedStatuses);

    public bool MatchesStatus(int actualStatus) => AllowedStatuses.Contains(actualStatus);

    public bool IsBaseline =>
        string.Equals(ScenarioType, PatientSecurityConstants.ScenarioBaseline, StringComparison.OrdinalIgnoreCase);

    public static PatientSecurityEntry? FromRow(Dictionary<string, string> row, string sheetCategory)
    {
        if (!row.TryGetValue(PatientSecurityConstants.TestCaseIdColumn, out var testCaseId)
            || string.IsNullOrWhiteSpace(testCaseId))
            return null;

        if (!row.TryGetValue(PatientSecurityConstants.ApiIdColumn, out var apiId)
            || string.IsNullOrWhiteSpace(apiId)
            || !PatientSecurityConstants.PatientApiIds.Contains(apiId.Trim()))
            return null;

        apiId = apiId.Trim();
        if (!PatientSecurityConstants.EndpointByApiId.TryGetValue(apiId, out var definition))
            return null;

        var mutation = row.GetValueOrDefault(PatientSecurityConstants.MutationAppliedColumn)?.Trim();
        var scenarioType = PatientSecurityScenarioMapper.MapScenarioType(
            testCaseId.Trim(),
            sheetCategory,
            mutation);

        if (scenarioType == null)
            return null;

        var allowed = ParseAllowedStatuses(row.GetValueOrDefault(PatientSecurityConstants.ExpectedStatusColumn));
        if (allowed.Count == 0 && !string.Equals(scenarioType, PatientSecurityConstants.ScenarioBaseline, StringComparison.OrdinalIgnoreCase))
            allowed = [401];

        // OpenAPI-derived method in code is authoritative; matrix rows may list POST generically.
        var httpMethod = definition.DefaultHttpMethod;
        var endpointPath = row.GetValueOrDefault(PatientSecurityConstants.EndpointColumn, string.Empty).Trim();
        var automationStatus = row.GetValueOrDefault(PatientSecurityConstants.AutomationStatusColumn, "Not Run").Trim();

        return new PatientSecurityEntry
        {
            TestCaseId = testCaseId.Trim(),
            ApiId = apiId,
            Category = sheetCategory,
            HttpMethod = httpMethod,
            EndpointPath = endpointPath,
            EndpointKey = definition.EndpointKey,
            ScenarioType = scenarioType,
            Role = PatientSecurityConstants.DefaultRole,
            ExpectedStatus = allowed.Count > 0 ? allowed[0] : 200,
            AllowedStatuses = allowed.Count > 0 ? allowed : [200],
            MutationApplied = mutation,
            TamperedRequest = NullIfEmpty(row.GetValueOrDefault(PatientSecurityConstants.TamperedRequestColumn)),
            UrlSegmentKeys = definition.UrlSegmentKeys,
            QueryParamKeys = definition.QueryParamKeys,
            BodyKey = definition.BodyKey,
            IsDestructive = string.Equals(apiId, "API-010", StringComparison.OrdinalIgnoreCase),
            SkipAutomation = string.Equals(automationStatus, "Skip", StringComparison.OrdinalIgnoreCase)
                || string.Equals(automationStatus, "Blocked", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static List<int> ParseAllowedStatuses(string? raw)
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(raw))
            return result;

        foreach (Match match in System.Text.RegularExpressions.Regex.Matches(raw, @"\b(\d{3})\b"))
        {
            if (int.TryParse(match.Groups[1].Value, out var code) && !result.Contains(code))
                result.Add(code);
        }

        return result;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
