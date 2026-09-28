namespace EnterpriseApiAutomationFramework.Core.Security.Authentication;

/// <summary>
/// Constants for Excel-driven API Security Authentication (JWT-gate) tests.
/// Separate from AuthorizationMatrix so existing authZ scenarios stay untouched.
/// </summary>
public static class ApiSecurityAuthConstants
{
    public const string MatrixExcelFile = "ApiSecurityAuthMatrix.xlsx";
    public const string AuthenticationSheet = "Authentication";

    public const string TestCaseIdColumn = "TestCaseId";
    public const string EndpointKeyColumn = "EndpointKey";
    public const string HttpMethodColumn = "HttpMethod";
    public const string ScenarioTypeColumn = "ScenarioType";
    public const string RoleColumn = "Role";
    public const string ExpectedStatusColumn = "ExpectedStatus";
    public const string RequiresSessionColumn = "RequiresSession";
    public const string HeaderKeysColumn = "HeaderKeys";
    public const string QueryParamKeysColumn = "QueryParamKeys";
    public const string EnabledColumn = "Enabled";
    public const string DescriptionColumn = "Description";

    public const string DefaultRole = "SuperAdmin";
    public const string SessionFeatureName = "User API Testing";

    public const int StatusUnauthorized = 401;

    public const string ScenarioNoAuthHeader = "NoAuthHeader";
    public const string ScenarioEmptyBearer = "EmptyBearer";
    public const string ScenarioInvalidToken = "InvalidToken";
    public const string ScenarioMalformedToken = "MalformedToken";
    public const string ScenarioExpiredToken = "ExpiredToken";
    public const string ScenarioTamperedToken = "TamperedToken";
    public const string ScenarioMissingBearer = "MissingBearer";

    public static readonly IReadOnlyList<string> SupportedScenarioTypes =
    [
        ScenarioNoAuthHeader,
        ScenarioEmptyBearer,
        ScenarioInvalidToken,
        ScenarioMalformedToken,
        ScenarioExpiredToken,
        ScenarioTamperedToken,
        ScenarioMissingBearer
    ];
}
