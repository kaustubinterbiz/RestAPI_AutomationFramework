namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Excel workbook and sheet names for config migrated from JSON (Phase 1+).
/// appsettings.json paths stay unchanged; readers use these Excel targets.
/// </summary>
public static class TestConfigDefaults
{
    public const string EndpointExcelFile = "RequestEndPoint.xlsx";
    public const string EndpointSheet = "Endpoints";
    public const string EndpointResponseSheet = "Endpoint_Response";

    public const string LoginExcelFile = "LoginRequest.xlsx";
    public const string LoginRolesSheet = "Roles";
    public const string LoginResponseSheet = "Login_Response";
    public const string RoleGroupsSheet = "RoleGroups";

    public const string ParentRoleColumn = "ParentRole";
    public const string ChildRoleColumn = "ChildRole";
    public const string ExecutionOrderColumn = "ExecutionOrder";
    public const string EnabledColumn = "Enabled";

    public const string DefaultAddMemberRoleGroup = "AddMemberRole";

    /// <summary>Default child roles for AddMemberRole (Excel RoleGroups sheet source of truth).</summary>
    public static readonly IReadOnlyList<(string Role, int Order)> DefaultAddMemberChildRoles =
    [
        ("SuperAdmin", 1),
        ("HospitalRole", 2),
        ("OrganizationRole", 3)
    ];

    public const string BodyExcelFile = "RequestBody.xlsx";
    public const string BodyResponseSheet = "Body_Response";
    public const string FieldColumn = "Field";
    public const string BodyRawJsonMarker = "_body";

    public const string KeyColumn = "Key";
    public const string ValueColumn = "Value";
    public const string RoleColumn = "Role";
    public const string TokenSnippetColumn = "TokenSnippet";
    public const string HttpStatusColumn = "HttpStatus";
    public const string ResponseSnippetColumn = "ResponseSnippet";
    public const string UpdatedAtColumn = "UpdatedAt";

    public static readonly IReadOnlyList<string> LoginCredentialColumns =
    [
        "grant_type",
        "client_id",
        "scope",
        "username",
        "password"
    ];
}
