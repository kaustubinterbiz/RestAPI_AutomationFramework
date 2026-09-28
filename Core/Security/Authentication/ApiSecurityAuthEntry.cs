namespace EnterpriseApiAutomationFramework.Core.Security.Authentication;

/// <summary>
/// One row from ApiSecurityAuthMatrix.xlsx → Authentication sheet.
/// </summary>
public sealed class ApiSecurityAuthEntry
{
    public required string TestCaseId { get; init; }
    public required string EndpointKey { get; init; }
    public required string HttpMethod { get; init; }
    public required string ScenarioType { get; init; }
    public required string Role { get; init; }
    /// <summary>Primary expected status (first value when multiple are allowed).</summary>
    public required int ExpectedStatus { get; init; }

    /// <summary>
    /// Allowed HTTP statuses for a rejection (e.g. 401 and 400).
    /// Excel may use a single number or pipe list: <c>401|400</c>.
    /// </summary>
    public required IReadOnlyList<int> AllowedStatuses { get; init; }

    public bool RequiresSession { get; init; }
    public string? HeaderKeys { get; init; }
    public string? QueryParamKeys { get; init; }
    public string? Description { get; init; }

    public string Label =>
        $"{TestCaseId} | {ScenarioType} | {HttpMethod} {EndpointKey}";

    public string ExpectedStatusDisplay =>
        string.Join("|", AllowedStatuses);

    public bool MatchesStatus(int actualStatus) =>
        AllowedStatuses.Contains(actualStatus);

    public static ApiSecurityAuthEntry? FromRow(Dictionary<string, string> row)
    {
        if (!row.TryGetValue(ApiSecurityAuthConstants.TestCaseIdColumn, out var testCaseId)
            || string.IsNullOrWhiteSpace(testCaseId))
            return null;

        if (!row.TryGetValue(ApiSecurityAuthConstants.EndpointKeyColumn, out var endpointKey)
            || string.IsNullOrWhiteSpace(endpointKey))
            return null;

        if (!row.TryGetValue(ApiSecurityAuthConstants.ScenarioTypeColumn, out var scenarioType)
            || string.IsNullOrWhiteSpace(scenarioType))
            return null;

        var allowed = ParseAllowedStatuses(
            row.GetValueOrDefault(ApiSecurityAuthConstants.ExpectedStatusColumn));
        if (allowed.Count == 0)
            return null;

        var role = row.GetValueOrDefault(ApiSecurityAuthConstants.RoleColumn, ApiSecurityAuthConstants.DefaultRole)
            .Trim();
        if (string.IsNullOrWhiteSpace(role) || role == "-")
            role = ApiSecurityAuthConstants.DefaultRole;

        return new ApiSecurityAuthEntry
        {
            TestCaseId = testCaseId.Trim(),
            EndpointKey = endpointKey.Trim(),
            HttpMethod = row.GetValueOrDefault(ApiSecurityAuthConstants.HttpMethodColumn, "GET").Trim(),
            ScenarioType = scenarioType.Trim(),
            Role = role,
            ExpectedStatus = allowed[0],
            AllowedStatuses = allowed,
            RequiresSession = IsTruthy(row.GetValueOrDefault(ApiSecurityAuthConstants.RequiresSessionColumn)),
            HeaderKeys = NullIfDash(row.GetValueOrDefault(ApiSecurityAuthConstants.HeaderKeysColumn)),
            QueryParamKeys = NullIfDash(row.GetValueOrDefault(ApiSecurityAuthConstants.QueryParamKeysColumn)),
            Description = NullIfDash(row.GetValueOrDefault(ApiSecurityAuthConstants.DescriptionColumn))
        };
    }

    private static List<int> ParseAllowedStatuses(string? raw)
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(raw))
            return result;

        foreach (var part in raw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var code))
                result.Add(code);
        }

        return result;
    }

    private static bool IsTruthy(string? value) =>
        string.Equals(value, "Yes", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "Y", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "True", StringComparison.OrdinalIgnoreCase)
        || value == "1";

    private static string? NullIfDash(string? value) =>
        string.IsNullOrWhiteSpace(value) || value == "-" ? null : value.Trim();
}
