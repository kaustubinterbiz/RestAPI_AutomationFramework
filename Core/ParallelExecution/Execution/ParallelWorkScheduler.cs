using System.Collections.Concurrent;
using System.Diagnostics;
using EnterpriseApiAutomationFramework.Core.ParallelExecution.Models;

namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Execution;

/// <summary>
/// Schedules execution units on the thread pool with true concurrent worker starts.
/// </summary>
public static class ParallelWorkScheduler
{
    public static async Task<IReadOnlyList<FeatureExecutionResult>> RunAsync(
        IReadOnlyList<ExecutionUnitDescriptor> units,
        int maxDegreeOfParallelism,
        Func<ExecutionUnitDescriptor, CancellationToken, Task<FeatureExecutionResult>> executeUnitAsync,
        bool failFast,
        CancellationToken cancellationToken = default)
    {
        if (units.Count == 0)
            return Array.Empty<FeatureExecutionResult>();

        var effectiveParallelism = ResolveParallelism(units.Count, maxDegreeOfParallelism);
        var results = new ConcurrentBag<FeatureExecutionResult>();
        using var failFastCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = effectiveParallelism,
            CancellationToken = failFastCts.Token
        };

        await Parallel.ForEachAsync(
            units,
            parallelOptions,
            async (unit, ct) =>
            {
                var result = await executeUnitAsync(unit, ct).ConfigureAwait(false);
                results.Add(result);

                if (failFast && result.Status == ExecutionStatus.Failed)
                    await failFastCts.CancelAsync();
            });

        return results.OrderBy(r => r.ModuleId, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static int ResolveParallelism(int unitCount, int configuredMax)
    {
        if (unitCount <= 0)
            return 1;

        if (configuredMax <= 0)
            return unitCount;

        return Math.Min(unitCount, configuredMax);
    }

    public static ParallelRunStatistics ComputeStatistics(
        IReadOnlyList<FeatureExecutionResult> results,
        DateTime wallClockStartUtc,
        DateTime wallClockEndUtc,
        int maxConcurrentWorkers)
    {
        var wallMs = (wallClockEndUtc - wallClockStartUtc).TotalMilliseconds;
        var sumMs = results.Sum(r => r.Duration.TotalMilliseconds);
        var efficiency = sumMs <= 0 ? 0 : Math.Min(100, sumMs / (wallMs * Math.Max(1, maxConcurrentWorkers)) * 100);
        var saved = Math.Max(0, sumMs - wallMs);
        var overlapped = results.Count > 1 && wallMs < sumMs * 0.85;

        return new ParallelRunStatistics
        {
            TotalUnits = results.Count,
            MaxConcurrentWorkers = maxConcurrentWorkers,
            WallClockDurationMs = wallMs,
            SumOfUnitDurationsMs = sumMs,
            ParallelismEfficiencyPercent = Math.Round(efficiency, 2),
            TimeSavedMs = Math.Round(saved, 2),
            AchievedConcurrency = overlapped
        };
    }
}
