namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Models;

/// <summary>
/// A parallelizable unit: either an entire feature file or a single scenario.
/// </summary>
public sealed class ExecutionUnitDescriptor
{
    public required string Id { get; init; }
    public required string ModuleName { get; init; }
    public ExecutionUnitType UnitType { get; init; }
    public string? ParentFeatureName { get; init; }
    public string? FeatureFilePath { get; init; }
    public required string TestFilterExpression { get; init; }
}
