namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Models;

public sealed class ParallelRunStatistics
{
    public int TotalUnits { get; init; }
    public int MaxConcurrentWorkers { get; init; }
    public double WallClockDurationMs { get; init; }
    public double SumOfUnitDurationsMs { get; init; }
    public double ParallelismEfficiencyPercent { get; init; }
    public double TimeSavedMs { get; init; }
    public bool AchievedConcurrency { get; init; }
}
