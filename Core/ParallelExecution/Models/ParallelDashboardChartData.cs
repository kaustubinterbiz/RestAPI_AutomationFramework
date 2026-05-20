namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Models;

/// <summary>
/// Aggregated chart datasets for the parallel consolidated HTML dashboard only.
/// </summary>
public sealed class ParallelDashboardChartData
{
    public required PieSliceDataset Features { get; init; }
    public required PieSliceDataset Scenarios { get; init; }
    public required PieSliceDataset Steps { get; init; }
    public required PieSliceDataset ExecutionResults { get; init; }
    public required IReadOnlyList<TimelineEntry> Timeline { get; init; }
    public long RunStartUtcTicks { get; init; }
    public long RunEndUtcTicks { get; init; }
    public double TotalRunDurationMs { get; init; }
}

public sealed class PieSliceDataset
{
    public required IReadOnlyList<string> Labels { get; init; }
    public required IReadOnlyList<int> Values { get; init; }
    public required IReadOnlyList<string> Colors { get; init; }
}

public sealed class TimelineEntry
{
    public required string Label { get; init; }
    public required string Lane { get; init; }
    public int? WorkerOsProcessId { get; init; }
    public ExecutionStatus Status { get; init; }
    public double StartOffsetMs { get; init; }
    public double EndOffsetMs { get; init; }
    public double DurationMs { get; init; }
    public DateTime StartTimeUtc { get; init; }
    public DateTime EndTimeUtc { get; init; }
}
