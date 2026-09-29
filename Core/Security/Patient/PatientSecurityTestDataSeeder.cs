using EnterpriseApiAutomationFramework.Core.Helpers;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.Core.Security.Patient;

/// <summary>
/// Seeds Org-A / Org-B patient identifiers into Endpoint_Response cache before Patient security runs.
/// </summary>
public static class PatientSecurityTestDataSeeder
{
    private static int _seeded;

    public static async Task EnsureSeededAsync(ScenarioContext context)
    {
        if (Interlocked.CompareExchange(ref _seeded, 1, 0) != 0)
            return;

        PatientSecurityBootstrap.EnsurePatientInfrastructure();

        var authHelper = new PatientSecurityAuthHelper();
        await authHelper.PrepareSessionAsync(context, PatientSecurityTestDataConfig.RoleUserA);

        var orgAPatient = FirstNonEmpty(
            PatientSecurityTestDataConfig.OrgAPatientId,
            PatientSecurityTestDataConfig.RandomGuid);

        UpsertIfNotEmpty("A_OWNED_PATIENT_ID", orgAPatient);
        UpsertIfNotEmpty("dataType", PatientSecurityTestDataConfig.DataType);
        UpsertIfNotEmpty("ServiceRequestId", PatientSecurityTestDataConfig.ServiceRequestId);

        var orgBPatient = FirstNonEmpty(
            PatientSecurityTestDataConfig.OrgBPatientId,
            PatientSecurityTestDataConfig.RandomGuid);

        UpsertIfNotEmpty("B_OTHER_ORG_PATIENT_ID", orgBPatient);

        UpsertIfNotEmpty("ValidateBusinessUnitId", PatientSecurityTestDataConfig.OrgBBusinessUnitId);
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "00000000-0000-0000-0000-000000000099";

    private static void UpsertIfNotEmpty(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        ExcelConfigWriter.UpsertEndpointResponse(key, value);
    }
}
