using System.Text;
using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Helpers;
using NUnit.Framework;
using Reqnroll;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Security.Patient;

public sealed class PatientSecurityExecutor
{
    private readonly PatientSecurityAuthHelper _authHelper = new();
    private readonly PatientSecurityRequestExecutor _requestExecutor = new();

    public async Task ExecuteRowAsync(ScenarioContext context, PatientSecurityEntry entry)
    {
        if (entry.IsDestructive && !PatientSecurityTestDataConfig.AllowDestructiveTests)
        {
            AuthorizationExecutionTracker.RecordSuccess(
                context,
                $"{entry.Label} | SKIPPED destructive",
                0,
                entry.ExpectedStatus);
            return;
        }

        await PatientSecurityTestDataSeeder.EnsureSeededAsync(context);

        string? baseline = null;
        if (PatientSecurityAuthHelper.NeedsBaselineLogin(entry.ScenarioType)
            || entry.ScenarioType is PatientSecurityConstants.ScenarioIdorCrossOrg
                or PatientSecurityConstants.ScenarioInputValidation)
        {
            await _authHelper.PrepareSessionAsync(context, entry.Role);
            baseline = TokenManager.AccessToken;
        }

        var authState = _authHelper.BuildAuthState(entry.ScenarioType, baseline);

        Dictionary<string, string>? segmentOverrides = null;
        string? bodyOverride = null;

        if (string.Equals(entry.ScenarioType, PatientSecurityConstants.ScenarioIdorCrossOrg, StringComparison.OrdinalIgnoreCase))
            segmentOverrides = PatientSecurityMutationHelper.BuildIdorSegmentOverrides(entry);

        if (string.Equals(entry.ScenarioType, PatientSecurityConstants.ScenarioInputValidation, StringComparison.OrdinalIgnoreCase))
            bodyOverride = PatientSecurityMutationHelper.BuildValidationBodyOverride(entry);

        RestResponse response = await _requestExecutor.ExecuteAsync(
            entry,
            authState,
            bodyOverride,
            segmentOverrides,
            queryOverrides: segmentOverrides);

        var actual = (int)response.StatusCode;
        var phiIssue = PatientSecurityPhiAssert.ValidateNoSensitiveLeak(response.Content);
        var foreignPatient = PatientSecurityTestDataConfig.OrgBPatientId;
        if (!string.IsNullOrWhiteSpace(foreignPatient)
            && PatientSecurityPhiAssert.ContainsForeignPatientData(response.Content, foreignPatient)
            && entry.ScenarioType == PatientSecurityConstants.ScenarioIdorCrossOrg)
        {
            RecordFailure(context, entry, actual, $"Cross-org PHI leak detected for patient {foreignPatient}.");
            return;
        }

        if (phiIssue != null && actual is >= 200 and < 300)
        {
            RecordFailure(context, entry, actual, phiIssue);
            return;
        }

        if (entry.MatchesStatus(actual))
        {
            AuthorizationExecutionTracker.RecordSuccess(context, entry.Label, actual, entry.ExpectedStatus);
            return;
        }

        RecordFailure(context, entry, actual, $"Expected [{entry.ExpectedStatusDisplay}], got {actual}. Body={Truncate(response.Content, 180)}");
    }

    public async Task ExecuteAllAsync(ScenarioContext context, IReadOnlyList<PatientSecurityEntry> rows)
    {
        if (rows.Count == 0)
            throw new InvalidOperationException("No Patient security rows to execute.");

        AuthorizationExecutionTracker.Begin(context);
        var summary = new StringBuilder();
        summary.AppendLine();
        summary.AppendLine("=====================================");
        summary.AppendLine("Patient Security — Run Summary");
        summary.AppendLine($"Total Rows : {rows.Count}");
        summary.AppendLine("=====================================");

        foreach (var entry in rows)
        {
            try
            {
                await ExecuteRowAsync(context, entry);
                var results = AuthorizationExecutionTracker.GetResults(context);
                var last = results.LastOrDefault(r => r.Label.StartsWith(entry.TestCaseId, StringComparison.OrdinalIgnoreCase));
                summary.AppendLine(last?.Passed == true ? $"PASS | {entry.Label}" : $"FAIL | {entry.Label}");
            }
            catch (Exception ex)
            {
                AuthorizationExecutionTracker.RecordFailure(
                    context, entry.Label, null, entry.ExpectedStatus, ex.Message);
                summary.AppendLine($"FAIL | {entry.Label} | {ex.Message}");
            }
        }

        summary.AppendLine("=====================================");
        var text = summary.ToString();
        Console.WriteLine(text);
        TestContext.Progress.WriteLine(text);
    }

    private static void RecordFailure(ScenarioContext context, PatientSecurityEntry entry, int actual, string error) =>
        AuthorizationExecutionTracker.RecordFailure(
            context, entry.Label, actual, entry.ExpectedStatus, error);

    private static string Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? string.Empty : value.Length <= max ? value : value[..max] + "...";
}
