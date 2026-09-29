namespace EnterpriseApiAutomationFramework.Core.Security.Reporting;

/// <summary>Removes aged timestamped report artifacts while preserving index and latest shortcuts.</summary>
public static class SecurityReportCleanup
{
    private static readonly HashSet<string> ProtectedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "index.html",
        "latest.html",
        "latest.md",
        SecurityRunManifest.ManifestFileName
    };

    public static void ApplyRetentionIfConfigured()
    {
        var settings = SecurityReportCollector.SecurityReportingSettings;
        if (settings.RetentionDays <= 0)
            return;

        var cutoff = DateTime.UtcNow.AddDays(-settings.RetentionDays);
        var outputDir = SecurityReportCollector.ResolvePath(settings.OutputPath);
        var runsDir = SecurityReportCollector.ResolvePath(settings.RunsPath);

        DeleteAgedFiles(outputDir, cutoff, "*.html");
        DeleteAgedFiles(outputDir, cutoff, "*.md");
        DeleteAgedFiles(runsDir, cutoff, "run_*.json");

        PruneManifest(cutoff);
    }

    private static void DeleteAgedFiles(string directory, DateTime cutoffUtc, string pattern)
    {
        if (!Directory.Exists(directory))
            return;

        foreach (var file in Directory.GetFiles(directory, pattern))
        {
            var name = Path.GetFileName(file);
            if (ProtectedFileNames.Contains(name))
                continue;

            if (File.GetLastWriteTimeUtc(file) < cutoffUtc)
                File.Delete(file);
        }
    }

    private static void PruneManifest(DateTime cutoffUtc)
    {
        var kept = SecurityRunManifest.LoadAll()
            .Where(r => r.ExecutedAtUtc.UtcDateTime >= cutoffUtc)
            .ToList();

        var path = SecurityRunManifest.GetManifestPath();
        if (kept.Count == 0)
        {
            if (File.Exists(path))
                File.Delete(path);
            return;
        }

        var json = System.Text.Json.JsonSerializer.Serialize(
            kept,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
