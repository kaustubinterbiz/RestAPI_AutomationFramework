namespace EnterpriseApiAutomationFramework.Core.Helpers;

public sealed class AuthorizationPermissionEntry
{
    public required string Role { get; init; }
    public required string Permission { get; init; }
    public required string EndpointKey { get; init; }
    public required string HttpMethod { get; init; }
    public bool Allowed { get; init; }
    public bool RequiresSession { get; init; }
    public string? HeaderKeys { get; init; }
    public string? BodyKey { get; init; }

    public int ExpectedStatus => Allowed
        ? AuthorizationMatrixReader.StatusOk
        : AuthorizationMatrixReader.StatusForbidden;

    public static AuthorizationPermissionEntry? FromRow(Dictionary<string, string> row)
    {
        if (!row.TryGetValue(AuthorizationMatrixReader.RoleColumn, out var role)
            || string.IsNullOrWhiteSpace(role))
            return null;

        if (!row.TryGetValue(AuthorizationMatrixReader.PermissionColumn, out var permission)
            || string.IsNullOrWhiteSpace(permission))
            return null;

        if (!row.TryGetValue(AuthorizationMatrixReader.EndpointKeyColumn, out var endpointKey)
            || string.IsNullOrWhiteSpace(endpointKey))
            return null;

        var method = row.GetValueOrDefault(AuthorizationMatrixReader.HttpMethodColumn, "GET");
        var allowed = IsTruthy(row.GetValueOrDefault(AuthorizationMatrixReader.AllowedColumn));
        var requiresSession = IsTruthy(row.GetValueOrDefault(AuthorizationMatrixReader.RequiresSessionColumn));

        return new AuthorizationPermissionEntry
        {
            Role = role.Trim(),
            Permission = permission.Trim(),
            EndpointKey = endpointKey.Trim(),
            HttpMethod = method.Trim(),
            Allowed = allowed,
            RequiresSession = requiresSession,
            HeaderKeys = NullIfDash(row.GetValueOrDefault(AuthorizationMatrixReader.HeaderKeysColumn)),
            BodyKey = NullIfDash(row.GetValueOrDefault("BodyKey"))
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
