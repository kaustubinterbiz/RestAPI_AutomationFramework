namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Models;

public sealed class ScenarioExecutionResult
{
    public required string ScenarioName { get; init; }
    public ExecutionStatus Status { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public string? StackTrace { get; init; }
}
