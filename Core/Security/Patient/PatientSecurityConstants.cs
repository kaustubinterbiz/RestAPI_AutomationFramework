namespace EnterpriseApiAutomationFramework.Core.Security.Patient;

public static class PatientSecurityConstants
{
    public const string MatrixExcelFile = "API_Security_Test_Matrix_P0.xlsx";
    public const string AuthenticationSheet = "02_Authentication";
    public const string IdorSheet = "03_IDOR_CrossOrg";
    public const string InputValidationSheet = "04_Input_Validation";

    public const string TestCaseIdColumn = "Test Case ID";
    public const string ApiIdColumn = "API ID";
    public const string HttpMethodColumn = "HTTP Method";
    public const string EndpointColumn = "Endpoint";
    public const string SecurityCategoryColumn = "Security Category";
    public const string MutationAppliedColumn = "Mutation Applied";
    public const string ExpectedStatusColumn = "Expected HTTP Status";
    public const string TamperedRequestColumn = "Tampered / Mutated Request";
    public const string AutomationStatusColumn = "Automation Status";

    public const string DefaultRole = "HospitalRole";
    public const string DefaultRoleUserB = "OrganizationRole";

    public const string ScenarioBaseline = "Baseline";
    public const string ScenarioNoAuthHeader = "NoAuthHeader";
    public const string ScenarioEmptyBearer = "EmptyBearer";
    public const string ScenarioInvalidToken = "InvalidToken";
    public const string ScenarioMalformedToken = "MalformedToken";
    public const string ScenarioExpiredToken = "ExpiredToken";
    public const string ScenarioTamperedSignature = "TamperedSignature";
    public const string ScenarioWrongIssuerAudience = "WrongIssuerAudience";
    public const string ScenarioMissingClaim = "MissingClaim";
    public const string ScenarioIdorCrossOrg = "IdorCrossOrg";
    public const string ScenarioInputValidation = "InputValidation";

    /// <summary>9 Patient P0 API groups from the security matrix.</summary>
    public static readonly IReadOnlySet<string> PatientApiIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "API-003", "API-005", "API-006", "API-009", "API-010",
        "API-011", "API-013", "API-016", "API-017"
    };

    public static readonly IReadOnlyDictionary<string, PatientEndpointDefinition> EndpointByApiId =
        new Dictionary<string, PatientEndpointDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["API-003"] = new("patientGetFhirData", "POST", "businessunitId=BusinessUnitId", null, "patientGetFhirData_Body"),
            ["API-005"] = new("patientGetByDataType", "GET", "patientId=A_OWNED_PATIENT_ID,dataType=dataType", null, null),
            ["API-006"] = new("patientUpdateByDataType", "PUT", "patientId=A_OWNED_PATIENT_ID,dataType=dataType", null, "patientUpdate_Body"),
            ["API-009"] = new("patientFacesheet", "POST", "patientId=A_OWNED_PATIENT_ID,businessunitId=BusinessUnitId", null, "patientFacesheet_Body"),
            ["API-010"] = new("patientDelete", "POST", "patientId=A_OWNED_PATIENT_ID", null, "patientDelete_Body"),
            ["API-011"] = new("patientAttribute", "GET", null, "businessunitId=BusinessUnitId,patientId=A_OWNED_PATIENT_ID,serviceRequestId=ServiceRequestId", null),
            ["API-013"] = new("patientBulkProviders", "POST", null, null, "patientBulkProviders_Body"),
            ["API-016"] = new("patientEmrSearch", "POST", null, null, "patientEmrSearch_Body"),
            ["API-017"] = new("patientEmrAddUpdate", "POST", null, null, "patientEmrAddUpdate_Body")
        };
}

public sealed record PatientEndpointDefinition(
    string EndpointKey,
    string DefaultHttpMethod,
    string? UrlSegmentKeys,
    string? QueryParamKeys,
    string? BodyKey);
