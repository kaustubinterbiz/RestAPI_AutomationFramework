using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Security.Patient;

public static class PatientSecurityTestDataConfig
{
    private const string Section = "PatientSecurityTestData";

    public static bool AllowDestructiveTests =>
        bool.TryParse(Get("PatientSecurityTestData:AllowDestructiveTests"), out var v) && v;

    public static string RoleUserA =>
        FirstNonEmpty(Get("PatientSecurityTestData:RoleUserA"), PatientSecurityConstants.DefaultRole);

    public static string RoleUserB =>
        FirstNonEmpty(Get("PatientSecurityTestData:RoleUserB"), PatientSecurityConstants.DefaultRoleUserB);

    public static string OrgAPatientId =>
        FirstNonEmpty(
            Get("PatientSecurityTestData:OrgAPatientId"),
            Get("A_OWNED_PATIENT_ID"),
            "00000000-0000-0000-0000-000000000001");

    public static string OrgBPatientId =>
        FirstNonEmpty(
            Get("PatientSecurityTestData:OrgBPatientId"),
            Get("B_OTHER_ORG_PATIENT_ID"),
            "00000000-0000-0000-0000-000000000002");

    public static string OrgABusinessUnitId =>
        FirstNonEmpty(Get("PatientSecurityTestData:OrgABusinessUnitId"), Get("BusinessUnitId"));

    public static string OrgBBusinessUnitId =>
        FirstNonEmpty(Get("PatientSecurityTestData:OrgBBusinessUnitId"), Get("ValidateBusinessUnitId"));

    public static string RandomGuid =>
        FirstNonEmpty(Get("random-guid"), "00000000-0000-0000-0000-000000000099");

    public static string DataType =>
        FirstNonEmpty(Get("PatientSecurityTestData:dataType"), Get("dataType"), "1");

    public static string ServiceRequestId =>
        FirstNonEmpty(Get("PatientSecurityTestData:ServiceRequestId"), Get("ServiceRequestId"), RandomGuid);

    private static string? Get(string key)
    {
        ConfigReaderNew.LoadConfig("appsettings.json");
        var value = ConfigReaderNew.GetValue(key);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
}
