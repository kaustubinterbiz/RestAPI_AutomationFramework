namespace EnterpriseApiAutomationFramework.Core.Helpers;

public sealed class AuthorizationMatrixEntry
{
    public required string EndpointKey { get; init; }
    public required string HttpMethod { get; init; }
    public required string Role { get; init; }
    public required int ExpectedStatus { get; init; }
    public bool RequiresSession { get; init; }
    public string? HeaderKeys { get; init; }
    public string? UrlPlaceholderKeys { get; init; }
    public string? TargetValue { get; init; }
    public string? QueryParamKeys { get; init; }
    public string? Description { get; init; }

    public static AuthorizationMatrixEntry? FromRow(Dictionary<string, string> row)
    {
        if (!row.TryGetValue(AuthorizationMatrixReader.EndpointKeyColumn, out var endpointKey)
            || string.IsNullOrWhiteSpace(endpointKey))
            return null;

        if (!row.TryGetValue(AuthorizationMatrixReader.RoleColumn, out var role)
            || string.IsNullOrWhiteSpace(role))
            return null;

        if (!int.TryParse(row.GetValueOrDefault(AuthorizationMatrixReader.ExpectedStatusColumn), out var expectedStatus))
            return null;

        var method = row.GetValueOrDefault(AuthorizationMatrixReader.HttpMethodColumn, "GET");
        var requiresSession = IsTruthy(row.GetValueOrDefault(AuthorizationMatrixReader.RequiresSessionColumn));

        return new AuthorizationMatrixEntry
        {
            EndpointKey = endpointKey.Trim(),
            HttpMethod = method.Trim(),
            Role = role.Trim(),
            ExpectedStatus = expectedStatus,
            RequiresSession = requiresSession,
            HeaderKeys = NullIfDash(row.GetValueOrDefault(AuthorizationMatrixReader.HeaderKeysColumn)),
            UrlPlaceholderKeys = NullIfDash(row.GetValueOrDefault(AuthorizationMatrixReader.UrlPlaceholderKeysColumn)),
            TargetValue = NullIfDash(row.GetValueOrDefault(AuthorizationMatrixReader.TargetValueColumn)),
            QueryParamKeys = NullIfDash(row.GetValueOrDefault(AuthorizationMatrixReader.QueryParamKeysColumn)),
            Description = NullIfDash(row.GetValueOrDefault(AuthorizationMatrixReader.DescriptionColumn))
        };
    }

    private static bool IsTruthy(string? value) =>
        string.Equals(value, "Yes", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "Y", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "True", StringComparison.OrdinalIgnoreCase)
        || value == "1";

    private static string? NullIfDash(string? value) =>
        string.IsNullOrWhiteSpace(value) || value == "-" ? null : value.Trim();
}
