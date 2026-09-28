using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnterpriseApiAutomationFramework.Core.Security.Reporting;

/// <summary>Merges per-worker security run snapshots into one consolidated report for parallel execution.</summary>
public static class SecurityRunMerger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static (string MarkdownPath, string HtmlPath)? MergeAndRender(string sessionId)
    {
        if (!SecurityReportCollector.IsEnabled)
            return null;

        var workerFiles = SecurityReportCollector.FindWorkerRunFiles(sessionId);
        if (workerFiles.Count == 0)
        {
            Console.WriteLine($"Security merge: no worker snapshots found for session {sessionId}.");
            return null;
        }

        var snapshots = workerFiles
            .Select(SecurityReportCollector.LoadRun)
            .Where(s => s != null)
            .Cast<SecurityRunSnapshot>()
            .ToList();

        if (snapshots.Count == 0)
        {
            Console.WriteLine($"Security merge: worker files unreadable for session {sessionId}.");
            return null;
        }

        var mergedScenarios = snapshots
            .SelectMany(s => s.Scenarios)
            .GroupBy(s => $"{s.FeatureName}::{s.ScenarioName}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(s => s.ScenarioName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (mergedScenarios.Count == 0)
        {
            Console.WriteLine($"Security merge: no security scenarios captured in session {sessionId}.");
            return null;
        }

        var firstMeta = snapshots[0].Metadata;
        var startedAt = snapshots.Min(s => s.Metadata.ExecutedAtUtc);
        var completedAt = DateTimeOffset.UtcNow;
        var settings = SecurityReportCollector.SecurityReportingSettings;
        var runsDir = SecurityReportCollector.ResolvePath(settings.RunsPath);
        Directory.CreateDirectory(runsDir);

        var mergedPath = Path.Combine(runsDir, $"run_{sessionId}.json");
        var mergedSnapshot = new SecurityRunSnapshot
        {
            Metadata = new SecurityRunMetadata
            {
                RunId = sessionId,
                ProjectName = firstMeta.ProjectName,
                Environment = firstMeta.Environment,
                ApiBaseUrl = firstMeta.ApiBaseUrl,
                AuthBaseUrl = firstMeta.AuthBaseUrl,
                Framework = firstMeta.Framework,
                ExecutedAtUtc = startedAt,
                CompletedAtUtc = completedAt,
                Duration = completedAt - startedAt,
                RunMode = SecurityReportingConstants.RunModeParallelBatch,
                TestFilter = firstMeta.TestFilter ?? "ParallelExecution",
                TrxFilePath = snapshots
                    .Select(s => s.Metadata.TrxFilePath)
                    .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p))
            },
            Scenarios = mergedScenarios
        };

        var json = JsonSerializer.Serialize(mergedSnapshot, JsonOptions);
        File.WriteAllText(mergedPath, json);

        foreach (var workerFile in workerFiles)
        {
            try { File.Delete(workerFile); }
            catch { /* best effort cleanup */ }
        }

        var paths = SecurityLivingReportBuilder.BuildAndRender(mergedPath);
        Console.WriteLine($"Security Consolidated Report (HTML): {paths.HtmlPath}");
        return paths;
    }
}
