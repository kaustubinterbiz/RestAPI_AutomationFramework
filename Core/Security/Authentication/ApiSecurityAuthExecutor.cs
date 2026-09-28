using System.Text;
using EnterpriseApiAutomationFramework.Core.Helpers;
using NUnit.Framework;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.Core.Security.Authentication;

/// <summary>
/// Runs all enabled Authentication security rows with soft-assert aggregation
/// (continues after failures; final assert via AuthorizationExecutionTracker).
/// </summary>
public sealed class ApiSecurityAuthExecutor
{
    private readonly ApiSecurityAuthHelper _helper;

    public ApiSecurityAuthExecutor(ApiSecurityAuthHelper? helper = null) =>
        _helper = helper ?? new ApiSecurityAuthHelper();

    public async Task ExecuteAllAuthenticationTestsAsync(ScenarioContext context)
    {
        var rows = ApiSecurityAuthMatrixReader.GetAuthenticationRows(enabledOnly: true);
        if (rows.Count == 0)
        {
            throw new InvalidOperationException(
                $"No enabled rows in '{ApiSecurityAuthConstants.MatrixExcelFile}' " +
                $"sheet '{ApiSecurityAuthConstants.AuthenticationSheet}'.");
        }

        AuthorizationExecutionTracker.Begin(context);
        var summary = new StringBuilder();
        summary.AppendLine();
        summary.AppendLine("=====================================");
        summary.AppendLine("API Security Authentication — Run Summary");
        summary.AppendLine($"Total Enabled Rows : {rows.Count}");
        summary.AppendLine("=====================================");

        // One baseline login per role for the whole ExcelLoop (faster + more stable).
        var rolesNeedingLogin = rows
            .Where(r => NeedsBaselineLogin(r.ScenarioType))
            .Select(r => r.Role)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var baselineTokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in rolesNeedingLogin)
        {
            try
            {
                baselineTokens[role] = await _helper.EnsureBaselineTokenAsync(context, role);
                summary.AppendLine($"LOGIN OK | role={role}");
            }
            catch (Exception ex)
            {
                summary.AppendLine($"LOGIN FAIL | role={role} | {ex.Message}");
                // Record failure against every row that needs this role, then still continue
                // so the final assert shows a clear picture.
                foreach (var entry in rows.Where(r =>
                             NeedsBaselineLogin(r.ScenarioType)
                             && string.Equals(r.Role, role, StringComparison.OrdinalIgnoreCase)))
                {
                    AuthorizationExecutionTracker.RecordFailure(
                        context,
                        entry.Label,
                        null,
                        entry.ExpectedStatus,
                        $"Baseline login failed for role '{role}': {ex.Message}");
                }
            }
        }

        foreach (var entry in rows)
        {
            var label = entry.Label;

            if (NeedsBaselineLogin(entry.ScenarioType)
                && !baselineTokens.ContainsKey(entry.Role))
            {
                // Already recorded as login failure above.
                summary.AppendLine($"SKIP | {label} | baseline login missing");
                continue;
            }

            try
            {
                baselineTokens.TryGetValue(entry.Role, out var baselineToken);
                var response = await _helper.ExecuteAsync(context, entry, baselineToken);
                var actual = (int)response.StatusCode;

                if (entry.MatchesStatus(actual))
                {
                    AuthorizationExecutionTracker.RecordSuccess(
                        context,
                        label,
                        actual,
                        entry.ExpectedStatus);

                    summary.AppendLine($"PASS | {label} | status={actual}");
                    continue;
                }

                var bodySnippet = Truncate(response.Content, 180);
                var error =
                    $"Expected HTTP [{entry.ExpectedStatusDisplay}], got {actual}. " +
                    $"Scenario={entry.ScenarioType}. Body={bodySnippet}";

                AuthorizationExecutionTracker.RecordFailure(
                    context,
                    label,
                    actual,
                    entry.ExpectedStatus,
                    error);

                summary.AppendLine(
                    $"FAIL | {label} | expected=[{entry.ExpectedStatusDisplay}] actual={actual}");
            }
            catch (Exception ex)
            {
                AuthorizationExecutionTracker.RecordFailure(
                    context,
                    label,
                    null,
                    entry.ExpectedStatus,
                    ex.Message);

                summary.AppendLine($"FAIL | {label} | exception={ex.Message}");
            }
        }

        summary.AppendLine("=====================================");
        var text = summary.ToString();
        Console.WriteLine(text);
        TestContext.Progress.WriteLine(text);
    }

    private static bool NeedsBaselineLogin(string scenarioType) =>
        !string.Equals(scenarioType, ApiSecurityAuthConstants.ScenarioNoAuthHeader, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(scenarioType, ApiSecurityAuthConstants.ScenarioEmptyBearer, StringComparison.OrdinalIgnoreCase);

    private static string Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Length <= max
                ? value
                : value[..max] + "...";
}
