using EnterpriseApiAutomationFramework.Core.ParallelExecution.Configuration;
using EnterpriseApiAutomationFramework.Core.ParallelExecution.Models;

namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Discovery;

public static class ParallelExecutionDiscovery
{
    public static IReadOnlyList<ExecutionUnitDescriptor> Discover(
        string projectRoot,
        ParallelExecutionSettings settings) =>
        settings.Granularity == ParallelGranularity.Scenario
            ? ScenarioModuleDiscovery.Discover(projectRoot)
            : FeatureModuleDiscovery.Discover(projectRoot);
}
