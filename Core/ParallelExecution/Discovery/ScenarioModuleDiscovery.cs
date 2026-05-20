using System.Text.RegularExpressions;
using EnterpriseApiAutomationFramework.Core.ParallelExecution.Models;

namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Discovery;

/// <summary>
/// Discovers individual SpecFlow scenarios from generated .feature.cs files for scenario-level parallelism.
/// </summary>
public static class ScenarioModuleDiscovery
{
    private static readonly Regex ScenarioMethodRegex = new(
        @"\[NUnit\.Framework\.DescriptionAttribute\(""([^""]+)""\)\]\s*public\s+void\s+(\w+)\s*\(",
        RegexOptions.Compiled | RegexOptions.Singleline);

    public static IReadOnlyList<ExecutionUnitDescriptor> Discover(string projectRoot)
    {
        var featuresDir = Path.Combine(projectRoot, "Features");
        if (!Directory.Exists(featuresDir))
            return Array.Empty<ExecutionUnitDescriptor>();

        var units = new List<ExecutionUnitDescriptor>();

        foreach (var featureCsPath in Directory.GetFiles(featuresDir, "*.feature.cs", SearchOption.TopDirectoryOnly))
        {
            var featureFilePath = featureCsPath.Replace(".feature.cs", ".feature", StringComparison.OrdinalIgnoreCase);
            if (!File.Exists(featureFilePath))
                continue;

            var featureContent = File.ReadAllText(featureFilePath);
            var featureTitle = FeatureModuleDiscovery.ExtractFeatureTitle(featureContent)
                ?? Path.GetFileNameWithoutExtension(featureFilePath);

            var generatedContent = File.ReadAllText(featureCsPath);
            var fileId = Path.GetFileNameWithoutExtension(featureFilePath);

            foreach (Match match in ScenarioMethodRegex.Matches(generatedContent))
            {
                var scenarioTitle = match.Groups[1].Value.Trim();
                var methodName = match.Groups[2].Value.Trim();
                var safeId = $"{fileId}_{methodName}";

                units.Add(new ExecutionUnitDescriptor
                {
                    Id = safeId,
                    ModuleName = scenarioTitle,
                    UnitType = ExecutionUnitType.Scenario,
                    ParentFeatureName = featureTitle,
                    FeatureFilePath = Path.GetRelativePath(projectRoot, featureFilePath),
                    TestFilterExpression = $"FullyQualifiedName~{methodName}"
                });
            }
        }

        return units.OrderBy(u => u.ParentFeatureName).ThenBy(u => u.ModuleName).ToList();
    }
}
