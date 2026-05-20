using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnterpriseApiAutomationFramework.Core.ParallelExecution.Configuration;
using EnterpriseApiAutomationFramework.Core.ParallelExecution.Models;

namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Reporting;

public sealed class ConsolidatedReportBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ParallelExecutionSettings _settings;
    private readonly string _projectRoot;

    public ConsolidatedReportBuilder(ParallelExecutionSettings settings, string projectRoot)
    {
        _settings = settings;
        _projectRoot = projectRoot;
    }

    public async Task<ConsolidatedRunReport> BuildAsync(
        string runId,
        DateTime startUtc,
        DateTime endUtc,
        IReadOnlyList<FeatureExecutionResult> modules,
        ParallelRunStatistics? parallelStatistics = null)
    {
        var outputDir = Path.Combine(
            ResolvePath(_settings.ConsolidatedReportPath),
            $"run_{runId}");
        Directory.CreateDirectory(outputDir);

        var metrics = ComputeMetrics(modules);
        var jsonPath = Path.Combine(outputDir, "consolidated-report.json");
        var htmlPath = Path.Combine(outputDir, "consolidated-dashboard.html");
        var tablePath = Path.Combine(outputDir, "consolidated-summary.txt");

        var report = new ConsolidatedRunReport
        {
            RunId = runId,
            StartTimeUtc = startUtc,
            EndTimeUtc = endUtc,
            TotalModules = modules.Count,
            PassedModules = metrics.PassedModules,
            FailedModules = metrics.FailedModules,
            TotalScenarios = metrics.TotalScenarios,
            PassedScenarios = metrics.PassedScenarios,
            FailedScenarios = metrics.FailedScenarios,
            SuccessRatePercent = metrics.SuccessRatePercent,
            AverageModuleDurationMs = metrics.AverageModuleDurationMs,
            MaxModuleDurationMs = metrics.MaxModuleDurationMs,
            MinModuleDurationMs = metrics.MinModuleDurationMs,
            Modules = modules,
            ParallelStatistics = parallelStatistics,
            JsonReportPath = jsonPath,
            HtmlReportPath = htmlPath,
            TableReportPath = tablePath
        };

        var chartData = ParallelDashboardAnalyticsCollector.Collect(report);
        var chartDataPath = Path.Combine(outputDir, "chart-data.json");

        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(report, JsonOptions));
        await File.WriteAllTextAsync(chartDataPath, JsonSerializer.Serialize(chartData, JsonOptions));
        await File.WriteAllTextAsync(htmlPath, ParallelDashboardHtmlRenderer.Render(report, chartData));
        await File.WriteAllTextAsync(tablePath, BuildTextTable(report));

        Console.WriteLine();
        Console.WriteLine("=== Parallel Execution Consolidated Report ===");
        Console.WriteLine($"Run ID           : {runId}");
        Console.WriteLine($"Modules          : {report.PassedModules}/{report.TotalModules} passed");
        Console.WriteLine($"Scenarios        : {report.PassedScenarios}/{report.TotalScenarios} passed");
        Console.WriteLine($"Success Rate     : {report.SuccessRatePercent:F1}%");
        Console.WriteLine($"Total Duration   : {report.TotalDuration.TotalSeconds:F1}s");
        if (parallelStatistics != null)
        {
            Console.WriteLine($"Concurrency      : {(parallelStatistics.AchievedConcurrency ? "YES" : "NO")} (max workers: {parallelStatistics.MaxConcurrentWorkers})");
            Console.WriteLine($"Time Saved (est) : {parallelStatistics.TimeSavedMs:F0} ms");
        }
        Console.WriteLine($"JSON Report      : {jsonPath}");
        Console.WriteLine($"Chart Data JSON  : {chartDataPath}");
        Console.WriteLine($"HTML Dashboard   : {htmlPath}");
        Console.WriteLine($"Table Summary    : {tablePath}");
        Console.WriteLine("==============================================");

        return report;
    }

    private static (int PassedModules, int FailedModules, int TotalScenarios, int PassedScenarios,
        int FailedScenarios, double SuccessRatePercent, double AverageModuleDurationMs,
        double MaxModuleDurationMs, double MinModuleDurationMs) ComputeMetrics(
        IReadOnlyList<FeatureExecutionResult> modules)
    {
        var passedModules = modules.Count(m => m.Status == ExecutionStatus.Success);
        var failedModules = modules.Count - passedModules;
        var allScenarios = modules.SelectMany(m => m.Scenarios).ToList();
        var passedScenarios = allScenarios.Count(s => s.Status == ExecutionStatus.Success);
        var failedScenarios = allScenarios.Count - passedScenarios;
        var totalScenarios = allScenarios.Count;
        var successRate = totalScenarios == 0
            ? (modules.Count == 0 ? 0 : (double)passedModules / modules.Count * 100)
            : (double)passedScenarios / totalScenarios * 100;

        var durations = modules.Select(m => m.Duration.TotalMilliseconds).ToList();
        var avg = durations.Count == 0 ? 0 : durations.Average();
        var max = durations.Count == 0 ? 0 : durations.Max();
        var min = durations.Count == 0 ? 0 : durations.Min();

        return (passedModules, failedModules, totalScenarios, passedScenarios, failedScenarios,
            successRate, avg, max, min);
    }

    private static string BuildTextTable(ConsolidatedRunReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PARALLEL EXECUTION CONSOLIDATED SUMMARY");
        sb.AppendLine($"Run ID: {report.RunId}");
        sb.AppendLine($"Started: {report.StartTimeUtc:O}");
        sb.AppendLine($"Ended:   {report.EndTimeUtc:O}");
        sb.AppendLine($"Duration: {report.TotalDuration}");
        sb.AppendLine();
        sb.AppendLine($"Modules Passed: {report.PassedModules}/{report.TotalModules}");
        sb.AppendLine($"Scenarios Passed: {report.PassedScenarios}/{report.TotalScenarios}");
        sb.AppendLine($"Success Rate: {report.SuccessRatePercent:F1}%");
        if (report.ParallelStatistics != null)
        {
            sb.AppendLine();
            sb.AppendLine("PARALLEL EXECUTION STATISTICS");
            sb.AppendLine($"Max Concurrent Workers : {report.ParallelStatistics.MaxConcurrentWorkers}");
            sb.AppendLine($"Wall-Clock Duration    : {report.ParallelStatistics.WallClockDurationMs:F0} ms");
            sb.AppendLine($"Sum of Unit Durations  : {report.ParallelStatistics.SumOfUnitDurationsMs:F0} ms");
            sb.AppendLine($"Time Saved (estimated) : {report.ParallelStatistics.TimeSavedMs:F0} ms");
            sb.AppendLine($"Concurrency Achieved   : {report.ParallelStatistics.AchievedConcurrency}");
        }
        sb.AppendLine();
        sb.AppendLine("MODULE RESULTS");
        sb.AppendLine(new string('-', 120));
        sb.AppendLine($"{"Module",-30} {"Status",-10} {"Duration",-12} {"Scenarios",-12} {"Exit",-6} {"Error"}");
        sb.AppendLine(new string('-', 120));

        foreach (var module in report.Modules)
        {
            var scenarioSummary = $"{module.Scenarios.Count(s => s.Status == ExecutionStatus.Success)}/{module.Scenarios.Count}";
            sb.AppendLine(
                $"{module.ModuleName,-30} {module.Status,-10} {module.Duration,-12} {scenarioSummary,-12} {module.ExitCode,-6} {Trim(module.ErrorSummary, 40)}");
        }

        sb.AppendLine(new string('-', 120));
        sb.AppendLine();
        sb.AppendLine("FAILED MODULE DETAILS");
        foreach (var failed in report.Modules.Where(m => m.Status == ExecutionStatus.Failed))
        {
            sb.AppendLine($"* {failed.ModuleName}");
            sb.AppendLine($"  Error: {failed.ErrorSummary}");
            foreach (var scenario in failed.Scenarios.Where(s => s.Status == ExecutionStatus.Failed))
            {
                sb.AppendLine($"  - {scenario.ScenarioName}: {scenario.ErrorMessage}");
            }
        }

        return sb.ToString();
    }

    private static string Trim(string? value, int max) =>
        string.IsNullOrEmpty(value) ? "-" : value.Length <= max ? value : value[..max] + "...";

    private string ResolvePath(string relativePath) =>
        Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.GetFullPath(Path.Combine(_projectRoot, relativePath));
}
