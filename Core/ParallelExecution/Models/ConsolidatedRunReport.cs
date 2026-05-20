namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Models;

public sealed class ConsolidatedRunReport
{
    public required string RunId { get; init; }
    public DateTime StartTimeUtc { get; init; }
    public DateTime EndTimeUtc { get; init; }
    public TimeSpan TotalDuration => EndTimeUtc - StartTimeUtc;
    public int TotalModules { get; init; }
    public int PassedModules { get; init; }
    public int FailedModules { get; init; }
    public int TotalScenarios { get; init; }
    public int PassedScenarios { get; init; }
    public int FailedScenarios { get; init; }
    public double SuccessRatePercent { get; init; }
    public double AverageModuleDurationMs { get; init; }
    public double MaxModuleDurationMs { get; init; }
    public double MinModuleDurationMs { get; init; }
    public required IReadOnlyList<FeatureExecutionResult> Modules { get; init; }
    public ParallelRunStatistics? ParallelStatistics { get; init; }
    public required string JsonReportPath { get; init; }
    public required string HtmlReportPath { get; init; }
    public required string TableReportPath { get; init; }
}
