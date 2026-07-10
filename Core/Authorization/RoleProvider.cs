using EnterpriseApiAutomationFramework.Core.Helpers;

namespace EnterpriseApiAutomationFramework.Core.Authorization;

/// <summary>
/// Supplies role credentials from LoginRequest.xlsx (Roles sheet).
/// </summary>
public static class RoleProvider
{
    public static bool IsGuestRole(string role) =>
        string.Equals(role, AuthorizationConstants.GuestRole, StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, AuthorizationConstants.NoLoginRole, StringComparison.OrdinalIgnoreCase);

    public static void ValidateRoleExists(string role)
    {
        if (IsGuestRole(role))
            return;

        ExcelConfigReader.ValidateChildRolesExistInRolesSheet(new[] { role });
    }

    public static string GetCredentialsJson(string role)
    {
        if (IsGuestRole(role))
            return string.Empty;

        return ExcelConfigReader.GetLoginCredentialsJson(role);
    }

    public static IReadOnlyList<string> GetConfiguredRolesFromMatrix()
    {
        AuthorizationConfigBootstrap.EnsureAuthorizationMatrixWorkbook();
        return AuthorizationMatrixReader.GetEndpointAccessRows()
            .Select(r => r.Role)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
