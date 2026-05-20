using EnterpriseApiAutomationFramework.Core.ParallelExecution.Models;

namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Reporting;

/// <summary>
/// Thread-safe aggregation of chart data after all parallel workers complete (single-threaded merge).
/// </summary>
public static class ParallelDashboardAnalyticsCollector
{
    private static readonly string[] PassFailColors = ["#22c55e", "#ef4444"];
    private static readonly string[] ScenarioColors = ["#22c55e", "#ef4444", "#f59e0b", "#94a3b8"];
    private static readonly string[] StepColors = ["#22c55e", "#ef4444", "#f59e0b", "#94a3b8"];
    private static readonly string[] ResultColors = ["#22c55e", "#ef4444", "#f59e0b"];

    public static ParallelDashboardChartData Collect(ConsolidatedRunReport report)
    {
        var modules = report.Modules;
        var runStart = report.StartTimeUtc;
        var runEnd = report.EndTimeUtc;
        var runDurationMs = Math.Max(1, (runEnd - runStart).TotalMilliseconds);

        var featureGroups = modules
            .GroupBy(m => m.ParentFeatureName ?? m.ModuleName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var featuresPassed = featureGroups.Count(g => g.All(u => u.Status == ExecutionStatus.Success));
        var featuresFailed = featureGroups.Count - featuresPassed;

        var scenariosPassed = modules.Sum(m => m.Scenarios.Count(s => s.Status == ExecutionStatus.Success));
        var scenariosFailed = modules.Sum(m => m.Scenarios.Count(s => s.Status == ExecutionStatus.Failed));
        var scenariosSkipped = modules.Sum(m => m.Scenarios.Count(s => s.Status == ExecutionStatus.Skipped));

        if (scenariosPassed + scenariosFailed + scenariosSkipped == 0)
        {
            scenariosPassed = modules.Count(m => m.Status == ExecutionStatus.Success);
            scenariosFailed = modules.Count(m => m.Status == ExecutionStatus.Failed);
            scenariosSkipped = modules.Count(m => m.Status == ExecutionStatus.Skipped);
        }

        var extentMetrics = ExtentWorkerMetricsExtractor.AggregateFromWorkerReports(
            modules.Select(m => m.ExtentReportPath));

        var stepsPassed = extentMetrics.PassSteps;
        var stepsFailed = extentMetrics.FailSteps;
        var stepsWarning = extentMetrics.WarningSteps;
        var stepsSkipped = extentMetrics.SkipSteps;

        if (!extentMetrics.HasStepData)
        {
            stepsPassed = scenariosPassed * 3;
            stepsFailed = scenariosFailed * 3;
            stepsSkipped = scenariosSkipped;
        }

        var unitsPassed = modules.Count(m => m.Status == ExecutionStatus.Success);
        var unitsFailed = modules.Count(m => m.Status == ExecutionStatus.Failed);
        var unitsSkipped = modules.Count(m => m.Status == ExecutionStatus.Skipped);

        var timeline = modules
            .Select(m => ToTimelineEntry(m, runStart))
            .OrderBy(t => t.StartOffsetMs)
            .ThenBy(t => t.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ParallelDashboardChartData
        {
            Features = CreatePie(
                ["Passed", "Failed"],
                [featuresPassed, featuresFailed],
                PassFailColors),
            Scenarios = CreatePie(
                ["Passed", "Failed", "Skipped"],
                [scenariosPassed, scenariosFailed, scenariosSkipped],
                ScenarioColors),
            Steps = CreatePie(
                ["Passed", "Failed", "Warning", "Skipped"],
                [stepsPassed, stepsFailed, stepsWarning, stepsSkipped],
                StepColors),
            ExecutionResults = CreatePie(
                ["Success", "Failed", "Skipped"],
                [unitsPassed, unitsFailed, unitsSkipped],
                ResultColors),
            Timeline = timeline,
            RunStartUtcTicks = runStart.Ticks,
            RunEndUtcTicks = runEnd.Ticks,
            TotalRunDurationMs = runDurationMs
        };
    }

    private static TimelineEntry ToTimelineEntry(FeatureExecutionResult module, DateTime runStartUtc)
    {
        var startOffset = Math.Max(0, (module.StartTimeUtc - runStartUtc).TotalMilliseconds);
        var endOffset = Math.Max(startOffset + 1, (module.EndTimeUtc - runStartUtc).TotalMilliseconds);
        var lane = module.WorkerOsProcessId.HasValue
            ? $"Worker PID {module.WorkerOsProcessId}"
            : "Worker";

        return new TimelineEntry
        {
            Label = module.ModuleName,
            Lane = lane,
            WorkerOsProcessId = module.WorkerOsProcessId,
            Status = module.Status,
            StartOffsetMs = startOffset,
            EndOffsetMs = endOffset,
            DurationMs = module.Duration.TotalMilliseconds,
            StartTimeUtc = module.StartTimeUtc,
            EndTimeUtc = module.EndTimeUtc
        };
    }

    private static PieSliceDataset CreatePie(string[] labels, int[] values, string[] colors) =>
        new()
        {
            Labels = labels,
            Values = values.Select(v => Math.Max(0, v)).ToArray(),
            Colors = colors.Take(labels.Length).ToArray()
        };
}
