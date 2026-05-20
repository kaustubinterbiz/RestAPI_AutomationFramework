namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Models;

public sealed class FeatureExecutionResult
{
    public required string ModuleId { get; init; }
    public required string ModuleName { get; init; }
    public ExecutionUnitType UnitType { get; init; } = ExecutionUnitType.Feature;
    public string? ParentFeatureName { get; init; }
    public ExecutionStatus Status { get; init; }
    public DateTime StartTimeUtc { get; init; }
    public DateTime EndTimeUtc { get; init; }
    public TimeSpan Duration => EndTimeUtc - StartTimeUtc;
    public int AttemptCount { get; init; }
    public int ExitCode { get; init; }
    /// <summary>OS process id of the isolated dotnet test worker (parallel runs only).</summary>
    public int? WorkerOsProcessId { get; init; }
    public string? ErrorSummary { get; init; }
    public string WorkerOutputDirectory { get; init; } = string.Empty;
    public string? TrxFilePath { get; init; }
    public string? LogFilePath { get; init; }
    public string? ExtentReportPath { get; init; }
    public IReadOnlyList<ScenarioExecutionResult> Scenarios { get; init; } = Array.Empty<ScenarioExecutionResult>();
    public IReadOnlyList<string> Logs { get; init; } = Array.Empty<string>();
}
