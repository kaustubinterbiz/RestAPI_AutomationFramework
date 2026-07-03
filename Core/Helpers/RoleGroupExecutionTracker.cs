using Reqnroll;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Tracks per-role execution results for Excel role-group loop scenarios.
/// </summary>
public static class RoleGroupExecutionTracker
{
    public const string ResultsKey = "RoleGroupExecutionResults";

    public sealed record RoleExecutionResult(string Role, bool Passed, string? ErrorMessage);

    public static void Begin(ScenarioContext context) =>
        context.Set(new List<RoleExecutionResult>(), ResultsKey);

    public static void RecordSuccess(ScenarioContext context, string role) =>
        Record(context, new RoleExecutionResult(role, true, null));

    public static void RecordFailure(ScenarioContext context, string role, Exception exception) =>
        Record(context, new RoleExecutionResult(role, false, exception.Message));

    public static IReadOnlyList<RoleExecutionResult> GetResults(ScenarioContext context)
    {
        if (!context.TryGetValue(ResultsKey, out List<RoleExecutionResult> results))
            return Array.Empty<RoleExecutionResult>();

        return results;
    }

    public static void AssertAllPassed(ScenarioContext context)
    {
        var results = GetResults(context).ToList();
        if (results.Count == 0)
        {
            throw new InvalidOperationException(
                "No role executions were recorded. Ensure the role-group step ran successfully.");
        }

        var failures = results.Where(r => !r.Passed).ToList();
        if (failures.Count == 0)
            return;

        var details = string.Join(
            Environment.NewLine,
            failures.Select(f => $"- {f.Role}: {f.ErrorMessage}"));

        throw new InvalidOperationException(
            $"One or more role executions failed:{Environment.NewLine}{details}");
    }

    private static void Record(ScenarioContext context, RoleExecutionResult result)
    {
        if (!context.TryGetValue(ResultsKey, out List<RoleExecutionResult> results))
        {
            results = new List<RoleExecutionResult>();
            context.Set(results, ResultsKey);
        }

        results.Add(result);
    }
}
