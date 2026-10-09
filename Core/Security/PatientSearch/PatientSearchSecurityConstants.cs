namespace EnterpriseApiAutomationFramework.Core.Security.PatientSearch;

public static class PatientSearchSecurityConstants
{
    public const string EndpointKey = "patientSearch";
    public const string EndpointKeyDynamicPath = "patientSearch_security_path";

    public const string LastResponseKey = "PatientSearchSecurity.LastResponse";
    public const string LastLabelKey = "PatientSearchSecurity.LastLabel";

    public static readonly int[] AuthRejectionStatuses = [401, 403];
}
