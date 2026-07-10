namespace EnterpriseApiAutomationFramework.Core.Authorization;

/// <summary>
/// Constants for Excel-driven authorization tests (AuthorizationMatrix.xlsx).
/// </summary>
public static class AuthorizationConstants
{
    public const string MatrixExcelFile = "AuthorizationMatrix.xlsx";
    public const string EndpointAccessSheet = "EndpointAccess";
    public const string TokenScenariosSheet = "TokenScenarios";
    public const string PermissionsSheet = "Permissions";

    public const string EndpointKeyColumn = "EndpointKey";
    public const string HttpMethodColumn = "HttpMethod";
    public const string RoleColumn = "Role";
    public const string ExpectedStatusColumn = "ExpectedStatus";
    public const string RequiresSessionColumn = "RequiresSession";
    public const string HeaderKeysColumn = "HeaderKeys";
    public const string UrlPlaceholderKeysColumn = "UrlPlaceholderKeys";
    public const string TargetValueColumn = "TargetValue";
    public const string QueryParamKeysColumn = "QueryParamKeys";
    public const string EnabledColumn = "Enabled";
    public const string DescriptionColumn = "Description";

    public const string ScenarioTypeColumn = "ScenarioType";
    public const string HeaderModeColumn = "HeaderMode";
    public const string BaseUrlColumn = "BaseUrl";

    public const string PermissionColumn = "Permission";
    public const string AllowedColumn = "Allowed";

    public const string GuestRole = "Guest";
    public const string NoLoginRole = "NoLogin";
    public const string SessionFeatureName = "User API Testing";

    public const int StatusOk = 200;
    public const int StatusUnauthorized = 401;
    public const int StatusForbidden = 403;

    public const string LastResponseKey = "AuthorizationLastResponse";
    public const string LastExpectedStatusKey = "AuthorizationLastExpectedStatus";
    public const string ExecutionResultsKey = "AuthorizationExecutionResults";

    public static readonly IReadOnlyList<string> SupportedPermissions =
        ["Create", "Read", "Update", "Delete"];

    public static readonly IReadOnlyList<string> SupportedTokenScenarios =
    [
        "ValidToken",
        "InvalidToken",
        "ExpiredToken",
        "EmptyToken",
        "TamperedToken",
        "MissingBearer"
    ];
}
