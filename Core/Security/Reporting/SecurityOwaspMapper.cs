namespace EnterpriseApiAutomationFramework.Core.Security.Reporting;

/// <summary>
/// Maps scenario / vulnerability types to OWASP API Top 10 (2023) categories and severity.
/// </summary>
public static class SecurityOwaspMapper
{
    public const string BrokenAuthentication = "API2:2023 Broken Authentication";
    public const string BrokenFunctionLevelAuthorization = "API5:2023 Broken Function Level Authorization";

    public static (string OwaspCategory, SecuritySeverity Severity, string RemediationKey) Map(
        string vulnerabilityType,
        string suite)
    {
        var normalized = vulnerabilityType.Trim();

        if (normalized.Equals("EndpointAccess", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("PermissionMatrix", StringComparison.OrdinalIgnoreCase)
            || suite.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                && IsRbacType(normalized))
        {
            return (BrokenFunctionLevelAuthorization, SecuritySeverity.High, "rbac");
        }

        return normalized.ToUpperInvariant() switch
        {
            "NOAUTHHEADER" or "EMPTYBEARER" or "MISSINGBEARER" or "EMPTYTOKEN" =>
                (BrokenAuthentication, SecuritySeverity.Critical, "missing_auth"),

            "INVALIDTOKEN" or "MALFORMEDTOKEN" or "EXPIREDTOKEN" or "TAMPEREDTOKEN" =>
                (BrokenAuthentication, SecuritySeverity.High, "invalid_jwt"),

            "VALIDTOKEN" =>
                (BrokenAuthentication, SecuritySeverity.Informational, "valid_control"),

            "PERMISSION" or "PERMISSIONMATRIX" =>
                (BrokenFunctionLevelAuthorization, SecuritySeverity.Critical, "permission"),

            _ when suite.Equals("Authorization", StringComparison.OrdinalIgnoreCase) =>
                (BrokenFunctionLevelAuthorization, SecuritySeverity.High, "rbac"),

            _ => (BrokenAuthentication, SecuritySeverity.High, "invalid_jwt")
        };
    }

    public static string FormatSeverity(SecuritySeverity severity) =>
        severity switch
        {
            SecuritySeverity.Critical => "Critical",
            SecuritySeverity.High => "High",
            SecuritySeverity.Medium => "Medium",
            SecuritySeverity.Low => "Low",
            SecuritySeverity.Informational => "Low",
            _ => "Medium"
        };

    private static bool IsRbacType(string type) =>
        type.Contains("Role", StringComparison.OrdinalIgnoreCase)
        || type.Equals("EndpointAccess", StringComparison.OrdinalIgnoreCase);
}
