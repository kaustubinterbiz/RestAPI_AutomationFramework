using EnterpriseApiAutomationFramework.Core.Authorization;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Reads authorization test matrix from AuthorizationMatrix.xlsx.
/// </summary>
public static class AuthorizationMatrixReader
{
    public const string EndpointKeyColumn = AuthorizationConstants.EndpointKeyColumn;
    public const string HttpMethodColumn = AuthorizationConstants.HttpMethodColumn;
    public const string RoleColumn = AuthorizationConstants.RoleColumn;
    public const string ExpectedStatusColumn = AuthorizationConstants.ExpectedStatusColumn;
    public const string RequiresSessionColumn = AuthorizationConstants.RequiresSessionColumn;
    public const string HeaderKeysColumn = AuthorizationConstants.HeaderKeysColumn;
    public const string UrlPlaceholderKeysColumn = AuthorizationConstants.UrlPlaceholderKeysColumn;
    public const string TargetValueColumn = AuthorizationConstants.TargetValueColumn;
    public const string QueryParamKeysColumn = AuthorizationConstants.QueryParamKeysColumn;
    public const string EnabledColumn = AuthorizationConstants.EnabledColumn;
    public const string DescriptionColumn = AuthorizationConstants.DescriptionColumn;
    public const string ScenarioTypeColumn = AuthorizationConstants.ScenarioTypeColumn;
    public const string HeaderModeColumn = AuthorizationConstants.HeaderModeColumn;
    public const string BaseUrlColumn = AuthorizationConstants.BaseUrlColumn;
    public const string PermissionColumn = AuthorizationConstants.PermissionColumn;
    public const string AllowedColumn = AuthorizationConstants.AllowedColumn;

    public const int StatusOk = AuthorizationConstants.StatusOk;
    public const int StatusForbidden = AuthorizationConstants.StatusForbidden;

    public static IReadOnlyList<AuthorizationMatrixEntry> GetEndpointAccessRows(bool enabledOnly = true)
    {
        AuthorizationMatrixCache.Load();
        var rows = AuthorizationMatrixCache.GetEndpointAccessRows();

        return rows
            .Where(r => !enabledOnly || IsEnabled(r))
            .Select(AuthorizationMatrixEntry.FromRow)
            .Where(e => e != null)
            .Cast<AuthorizationMatrixEntry>()
            .ToList();
    }

    public static IReadOnlyList<AuthorizationTokenScenarioEntry> GetTokenScenarioRows(bool enabledOnly = true)
    {
        AuthorizationMatrixCache.Load();
        var rows = AuthorizationMatrixCache.GetTokenScenarioRows();

        return rows
            .Where(r => !enabledOnly || IsEnabled(r))
            .Select(AuthorizationTokenScenarioEntry.FromRow)
            .Where(e => e != null)
            .Cast<AuthorizationTokenScenarioEntry>()
            .ToList();
    }

    public static IReadOnlyList<AuthorizationPermissionEntry> GetPermissionRows(bool enabledOnly = true)
    {
        AuthorizationMatrixCache.Load();
        var rows = AuthorizationMatrixCache.GetPermissionRows();

        return rows
            .Where(r => !enabledOnly || IsEnabled(r))
            .Select(AuthorizationPermissionEntry.FromRow)
            .Where(e => e != null)
            .Cast<AuthorizationPermissionEntry>()
            .ToList();
    }

    public static AuthorizationMatrixEntry? FindEndpointAccessRow(string endpointKey, string httpMethod, string role)
    {
        return GetEndpointAccessRows()
            .FirstOrDefault(r =>
                string.Equals(r.EndpointKey, endpointKey, StringComparison.OrdinalIgnoreCase)
                && string.Equals(r.HttpMethod, httpMethod, StringComparison.OrdinalIgnoreCase)
                && string.Equals(r.Role, role, StringComparison.OrdinalIgnoreCase));
    }

    public static AuthorizationTokenScenarioEntry? FindTokenScenario(string scenarioType, string role)
    {
        return GetTokenScenarioRows()
            .FirstOrDefault(r =>
                string.Equals(r.ScenarioType, scenarioType, StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrWhiteSpace(role)
                    || role == "-"
                    || string.Equals(r.Role, role, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsEnabled(Dictionary<string, string> row)
    {
        if (!row.TryGetValue(EnabledColumn, out var enabled) || string.IsNullOrWhiteSpace(enabled))
            return true;

        return string.Equals(enabled, "Yes", StringComparison.OrdinalIgnoreCase)
               || string.Equals(enabled, "Y", StringComparison.OrdinalIgnoreCase)
               || string.Equals(enabled, "True", StringComparison.OrdinalIgnoreCase)
               || enabled == "1";
    }
}
