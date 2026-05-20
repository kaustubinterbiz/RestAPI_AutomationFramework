using System.Text.RegularExpressions;

namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Reporting;

/// <summary>
/// Reads step/scenario counts from per-worker Extent HTML (statusGroup script) when available.
/// </summary>
internal static class ExtentWorkerMetricsExtractor
{
    private static readonly Regex StatusGroupRegex = new(
        @"passParent:(\d+),failParent:(\d+),warningParent:(\d+),skipParent:(\d+).*?" +
        @"passChild:(\d+),failChild:(\d+),warningChild:(\d+),skipChild:(\d+).*?" +
        @"passGrandChild:(\d+),failGrandChild:(\d+),warningGrandChild:(\d+),skipGrandChild:(\d+)",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public static ExtentAggregatedMetrics AggregateFromWorkerReports(IEnumerable<string?> extentReportPaths)
    {
        var totals = new ExtentAggregatedMetrics();

        foreach (var path in extentReportPaths.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            if (!File.Exists(path!))
                continue;

            try
            {
                var html = File.ReadAllText(path!);
                var match = StatusGroupRegex.Match(html);
                if (!match.Success)
                    continue;

                totals.PassFeatures += Parse(match.Groups[1]);
                totals.FailFeatures += Parse(match.Groups[2]);
                totals.WarningFeatures += Parse(match.Groups[3]);
                totals.SkipFeatures += Parse(match.Groups[4]);

                totals.PassScenarios += Parse(match.Groups[5]);
                totals.FailScenarios += Parse(match.Groups[6]);
                totals.WarningScenarios += Parse(match.Groups[7]);
                totals.SkipScenarios += Parse(match.Groups[8]);

                totals.PassSteps += Parse(match.Groups[9]);
                totals.FailSteps += Parse(match.Groups[10]);
                totals.WarningSteps += Parse(match.Groups[11]);
                totals.SkipSteps += Parse(match.Groups[12]);
            }
            catch
            {
                // Ignore unreadable worker extent files.
            }
        }

        return totals;
    }

    private static int Parse(Group group) =>
        int.TryParse(group.Value, out var value) ? value : 0;

    internal sealed class ExtentAggregatedMetrics
    {
        public int PassFeatures { get; set; }
        public int FailFeatures { get; set; }
        public int WarningFeatures { get; set; }
        public int SkipFeatures { get; set; }
        public int PassScenarios { get; set; }
        public int FailScenarios { get; set; }
        public int WarningScenarios { get; set; }
        public int SkipScenarios { get; set; }
        public int PassSteps { get; set; }
        public int FailSteps { get; set; }
        public int WarningSteps { get; set; }
        public int SkipSteps { get; set; }

        public bool HasStepData => PassSteps + FailSteps + WarningSteps + SkipSteps > 0;
    }
}
