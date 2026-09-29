using EnterpriseApiAutomationFramework.Core.Security.Reporting;

namespace EnterpriseApiAutomationFramework.SecurityReportRegenerator;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var mergeSessionId = ParseArg(args, "--mergeSession");
            if (!string.IsNullOrWhiteSpace(mergeSessionId))
            {
                var merged = SecurityRunMerger.MergeAndRender(mergeSessionId);
                if (merged == null)
                    return 1;

                Console.WriteLine($"Merged session: {mergeSessionId}");
                Console.WriteLine($"HTML: {merged.Value.HtmlPath}");
                Console.WriteLine($"Markdown: {merged.Value.MarkdownPath}");
                return 0;
            }

            var runId = ParseArg(args, "--runId") ?? (args.Length == 1 ? args[0] : null);
            if (string.IsNullOrWhiteSpace(runId))
            {
                Console.Error.WriteLine("Usage:");
                Console.Error.WriteLine("  dotnet run --project SecurityReportRegenerator -- --runId <RunId>");
                Console.Error.WriteLine("  dotnet run --project SecurityReportRegenerator -- --mergeSession <SessionId>");
                return 1;
            }

            var settings = SecurityReportCollector.SecurityReportingSettings;
            var runFile = Path.Combine(
                SecurityReportCollector.ResolvePath(settings.RunsPath),
                $"run_{runId}.json");

            if (!File.Exists(runFile))
            {
                Console.Error.WriteLine($"Run snapshot not found: {runFile}");
                return 1;
            }

            var (mdPath, htmlPath) = SecurityLivingReportBuilder.BuildAndRender(runFile);
            Console.WriteLine($"Regenerated from: {runFile}");
            Console.WriteLine($"HTML: {htmlPath}");
            Console.WriteLine($"Markdown: {mdPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Report operation failed: {ex.Message}");
            return 2;
        }
    }

    private static string? ParseArg(string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1];

            var prefix = name + "=";
            if (args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return args[i][prefix.Length..];
        }

        return null;
    }
}
