using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnterpriseApiAutomationFramework.Core.Security.Reporting;

/// <summary>Persists run history metadata for index page and scripts.</summary>
public static class SecurityRunManifest
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string ManifestFileName => "manifest.json";

    public static string GetManifestPath()
    {
        var settings = SecurityReportCollector.SecurityReportingSettings;
        return Path.Combine(SecurityReportCollector.ResolvePath(settings.RunsPath), ManifestFileName);
    }

    public static IReadOnlyList<SecurityRunManifestEntry> LoadAll()
    {
        var path = GetManifestPath();
        if (!File.Exists(path))
            return [];

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<SecurityRunManifestEntry>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static void AppendEntry(SecurityRunManifestEntry entry)
    {
        var path = GetManifestPath();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        var runs = LoadAll().ToList();
        runs.RemoveAll(r => string.Equals(r.RunId, entry.RunId, StringComparison.OrdinalIgnoreCase));
        runs.Insert(0, entry);

        var json = JsonSerializer.Serialize(runs, JsonOptions);
        File.WriteAllText(path, json);
    }
}
