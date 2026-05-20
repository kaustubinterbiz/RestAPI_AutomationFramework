using System.Text.RegularExpressions;
using EnterpriseApiAutomationFramework.Core.ParallelExecution.Models;

namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Discovery;

/// <summary>
/// Discovers SpecFlow feature files and builds NUnit filter expressions per module.
/// </summary>
public static class FeatureModuleDiscovery
{
    private static readonly Regex FeatureTitleRegex =
        new(@"^\s*Feature:\s*(.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

    public static IReadOnlyList<ExecutionUnitDescriptor> Discover(string projectRoot)
    {
        var featuresDir = Path.Combine(projectRoot, "Features");
        if (!Directory.Exists(featuresDir))
            return Array.Empty<ExecutionUnitDescriptor>();

        return Directory
            .GetFiles(featuresDir, "*.feature", SearchOption.TopDirectoryOnly)
            .Select(path => CreateDescriptor(path, projectRoot))
            .OrderBy(m => m.ModuleName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string? ExtractFeatureTitle(string featureFileContent)
    {
        var match = FeatureTitleRegex.Match(featureFileContent);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static ExecutionUnitDescriptor CreateDescriptor(string featureFilePath, string projectRoot)
    {
        var content = File.ReadAllText(featureFilePath);
        var featureTitle = ExtractFeatureTitle(content) ?? Path.GetFileNameWithoutExtension(featureFilePath);
        var className = ToSpecFlowFeatureClassName(featureTitle);
        var fileName = Path.GetFileNameWithoutExtension(featureFilePath);

        return new ExecutionUnitDescriptor
        {
            Id = fileName,
            ModuleName = featureTitle,
            UnitType = ExecutionUnitType.Feature,
            ParentFeatureName = featureTitle,
            FeatureFilePath = Path.GetRelativePath(projectRoot, featureFilePath),
            TestFilterExpression = $"FullyQualifiedName~{className}"
        };
    }

    /// <summary>
    /// Mirrors SpecFlow's default feature class naming (e.g. "User API Testing" -> UserAPITestingFeature).
    /// </summary>
    public static string ToSpecFlowFeatureClassName(string featureTitle)
    {
        var lettersAndDigits = new string(featureTitle.Where(char.IsLetterOrDigit).ToArray());
        return $"{lettersAndDigits}Feature";
    }
}
