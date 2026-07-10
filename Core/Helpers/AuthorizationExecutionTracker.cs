using Reqnroll;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Tracks authorization test execution results for Excel loop scenarios.
/// </summary>
public static class AuthorizationExecutionTracker
{
    public const string ResultsKey = "AuthorizationExecutionResults";

    public sealed record AuthorizationExecutionResult(
        string Label,
        bool Passed,
        int? ActualStatus,
        int? ExpectedStatus,
        string? ErrorMessage);

    public static void Begin(ScenarioContext context) =>
        context.Set(new List<AuthorizationExecutionResult>(), ResultsKey);

    public static void RecordSuccess(
        ScenarioContext context,
        string label,
        int actualStatus,
        int expectedStatus) =>
        Record(context, new AuthorizationExecutionResult(label, true, actualStatus, expectedStatus, null));

    public static void RecordFailure(
        ScenarioContext context,
        string label,
        int? actualStatus,
        int? expectedStatus,
        string errorMessage) =>
        Record(context, new AuthorizationExecutionResult(label, false, actualStatus, expectedStatus, errorMessage));

    public static IReadOnlyList<AuthorizationExecutionResult> GetResults(ScenarioContext context)
    {
        if (!context.TryGetValue(ResultsKey, out List<AuthorizationExecutionResult> results))
            return Array.Empty<AuthorizationExecutionResult>();

        return results;
    }

    public static void AssertAllPassed(ScenarioContext context)
    {
        var results = GetResults(context).ToList();
        if (results.Count == 0)
        {
            throw new InvalidOperationException(
                "No authorization executions were recorded. Ensure authorization steps ran successfully.");
        }

        var failures = results.Where(r => !r.Passed).ToList();
        if (failures.Count == 0)
            return;

        var details = string.Join(
            Environment.NewLine,
            failures.Select(f =>
                $"- {f.Label}: expected {f.ExpectedStatus}, actual {f.ActualStatus?.ToString() ?? "n/a"} — {f.ErrorMessage}"));

        throw new InvalidOperationException(
            $"One or more authorization executions failed:{Environment.NewLine}{details}");
    }

    private static void Record(ScenarioContext context, AuthorizationExecutionResult result)
    {
        if (!context.TryGetValue(ResultsKey, out List<AuthorizationExecutionResult> results))
        {
            results = new List<AuthorizationExecutionResult>();
            context.Set(results, ResultsKey);
        }

        results.Add(result);
    }
}
