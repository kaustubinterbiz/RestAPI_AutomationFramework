using EnterpriseApiAutomationFramework.Core.ParallelExecution.Configuration;
using EnterpriseApiAutomationFramework.Core.ParallelExecution.Discovery;
using EnterpriseApiAutomationFramework.Core.ParallelExecution.Models;
using EnterpriseApiAutomationFramework.Core.ParallelExecution.Reporting;

namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Execution;

/// <summary>
/// Coordinates true parallel execution via isolated OS processes and produces one consolidated report.
/// </summary>
public sealed class ParallelOrchestrator
{
    private readonly ParallelExecutionSettings _settings;
    private readonly string _projectPath;
    private readonly string _projectRoot;

    public ParallelOrchestrator(ParallelExecutionSettings settings, string projectPath, string projectRoot)
    {
        _settings = settings;
        _projectPath = projectPath;
        _projectRoot = projectRoot;
    }

    public async Task<ConsolidatedRunReport> RunAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
            throw new InvalidOperationException("Parallel execution is disabled in configuration.");

        var runStartUtc = DateTime.UtcNow;
        var runId = runStartUtc.ToString("yyyyMMdd_HHmmss");
        var units = ParallelExecutionDiscovery.Discover(_projectRoot, _settings);

        if (units.Count == 0)
            throw new InvalidOperationException("No execution units found. Check Features/ folder and Granularity setting.");

        Console.WriteLine($"[Parallel] Discovered {units.Count} unit(s) at '{_settings.Granularity}' granularity.");

        await BuildProjectAsync(cancellationToken);

        var executor = new ProcessIsolatedFeatureExecutor(_settings, _projectPath, _projectRoot);
        var maxAttempts = Math.Max(1, _settings.RetryCount + 1);
        var maxParallel = ParallelWorkScheduler.ResolveParallelism(units.Count, _settings.MaxDegreeOfParallelism);

        Console.WriteLine($"[Parallel] Starting {units.Count} worker process(es) with max concurrency = {maxParallel}.");

        var results = await ParallelWorkScheduler.RunAsync(
            units,
            _settings.MaxDegreeOfParallelism,
            (unit, ct) => ExecuteUnitWithRetryAsync(
                executor, unit, maxAttempts, _settings.RetryDelayMilliseconds, ct),
            _settings.FailFast,
            cancellationToken);

        var runEndUtc = DateTime.UtcNow;
        var statistics = ParallelWorkScheduler.ComputeStatistics(results, runStartUtc, runEndUtc, maxParallel);

        Console.WriteLine(
            $"[Parallel] Completed. Wall-clock: {statistics.WallClockDurationMs:F0}ms | " +
            $"Sum of units: {statistics.SumOfUnitDurationsMs:F0}ms | " +
            $"Concurrency achieved: {statistics.AchievedConcurrency}");

        var reportBuilder = new ConsolidatedReportBuilder(_settings, _projectRoot);
        return await reportBuilder.BuildAsync(runId, runStartUtc, runEndUtc, results, statistics);
    }

    private static async Task<FeatureExecutionResult> ExecuteUnitWithRetryAsync(
        ProcessIsolatedFeatureExecutor executor,
        ExecutionUnitDescriptor unit,
        int maxAttempts,
        int retryDelayMilliseconds,
        CancellationToken cancellationToken)
    {
        FeatureExecutionResult? lastResult = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            lastResult = await executor.ExecuteAsync(unit, attempt, cancellationToken);
            if (lastResult.Status == ExecutionStatus.Success)
                return lastResult;

            if (attempt < maxAttempts && retryDelayMilliseconds > 0)
                await Task.Delay(retryDelayMilliseconds, cancellationToken);
        }

        return lastResult!;
    }

    private async Task BuildProjectAsync(CancellationToken cancellationToken)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{_projectPath}\" --nologo",
            WorkingDirectory = _projectRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.Environment["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1";

        using var process = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet build.");

        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"dotnet build failed with exit code {process.ExitCode}.");
    }
}
