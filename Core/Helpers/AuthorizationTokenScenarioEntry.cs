namespace EnterpriseApiAutomationFramework.Core.Helpers;

public sealed class AuthorizationTokenScenarioEntry
{
    public required string ScenarioType { get; init; }
    public required string Role { get; init; }
    public required string EndpointKey { get; init; }
    public required int ExpectedStatus { get; init; }
    public string HeaderMode { get; init; } = "Bearer";
    public string BaseUrl { get; init; } = "Api";

    public static AuthorizationTokenScenarioEntry? FromRow(Dictionary<string, string> row)
    {
        if (!row.TryGetValue(AuthorizationMatrixReader.ScenarioTypeColumn, out var scenarioType)
            || string.IsNullOrWhiteSpace(scenarioType))
            return null;

        if (!row.TryGetValue(AuthorizationMatrixReader.EndpointKeyColumn, out var endpointKey)
            || string.IsNullOrWhiteSpace(endpointKey))
            return null;

        if (!int.TryParse(row.GetValueOrDefault(AuthorizationMatrixReader.ExpectedStatusColumn), out var expectedStatus))
            return null;

        var role = row.GetValueOrDefault(AuthorizationMatrixReader.RoleColumn, "-").Trim();

        return new AuthorizationTokenScenarioEntry
        {
            ScenarioType = scenarioType.Trim(),
            Role = role,
            EndpointKey = endpointKey.Trim(),
            ExpectedStatus = expectedStatus,
            HeaderMode = row.GetValueOrDefault(AuthorizationMatrixReader.HeaderModeColumn, "Bearer").Trim(),
            BaseUrl = row.GetValueOrDefault(AuthorizationMatrixReader.BaseUrlColumn, "Api").Trim()
        };
    }
}
