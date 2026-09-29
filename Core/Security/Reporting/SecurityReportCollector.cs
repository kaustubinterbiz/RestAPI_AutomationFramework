using System.Text.Json;
using System.Text.Json.Serialization;
using EnterpriseApiAutomationFramework.Core.Configurations;
using Microsoft.Extensions.Configuration;

namespace EnterpriseApiAutomationFramework.Core.Security.Reporting;

/// <summary>
/// Thread-safe run-level accumulator persisted to JSON for living report generation.
/// </summary>
public static class SecurityReportCollector
{
    private static readonly object Sync = new();
    private static SecurityRunSnapshot? _currentRun;
    private static string? _runFilePath;
    private static bool _reportRendered;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static bool IsEnabled => SecurityReportingSettings.Enabled;

    public static bool ShouldDeferRender =>
        string.Equals(
            Environment.GetEnvironmentVariable(SecurityReportingConstants.DeferRenderEnvVar),
            "true",
            StringComparison.OrdinalIgnoreCase);

    public static SecurityReportingSettings SecurityReportingSettings
    {
        get
        {
            var settings = new SecurityReportingSettings();
            AppConfiguration.Instance.GetSection("SecurityReporting").Bind(settings);
            return settings;
        }
    }

    public static string? CurrentRunFilePath
    {
        get
        {
            lock (Sync)
            {
                return _runFilePath;
            }
        }
    }

    public static string? CurrentRunId
    {
        get
        {
            lock (Sync)
            {
                return _currentRun?.Metadata.RunId;
            }
        }
    }

    public static int ScenarioCount
    {
        get
        {
            lock (Sync)
            {
                return _currentRun?.Scenarios.Count ?? 0;
            }
        }
    }

    public static void BeginRun()
    {
        if (!IsEnabled)
            return;

        lock (Sync)
        {
            _reportRendered = false;
            var settings = SecurityReportingSettings;
            var sessionId = Environment.GetEnvironmentVariable(SecurityReportingConstants.SessionIdEnvVar);
            var runId = !string.IsNullOrWhiteSpace(sessionId) ? sessionId : GenerateRunId();
            var runsDir = ResolvePath(settings.RunsPath);
            Directory.CreateDirectory(runsDir);

            _runFilePath = BuildRunFilePath(runsDir, runId);
            _currentRun = CreateSnapshot(runId, DateTimeOffset.UtcNow);
            PersistLocked();
        }
    }

    public static void RecordScenario(SecurityScenarioReport scenario)
    {
        if (!IsEnabled)
            return;

        lock (Sync)
        {
            _currentRun ??= CreateFallbackRun();
            _currentRun.Scenarios.Add(scenario);
            PersistLocked();
        }
    }

    public static void SetTrxFilePath(string? trxPath)
    {
        if (!IsEnabled)
            return;

        lock (Sync)
        {
            if (_currentRun == null)
                return;

            _currentRun = new SecurityRunSnapshot
            {
                Metadata = CopyMetadata(_currentRun.Metadata, trxFilePath: trxPath),
                Scenarios = _currentRun.Scenarios
            };
            PersistLocked();
        }
    }

    public static void FinalizeRun(string? runModeOverride = null)
    {
        if (!IsEnabled)
            return;

        lock (Sync)
        {
            if (_currentRun == null)
                return;

            var completedAt = DateTimeOffset.UtcNow;
            var duration = completedAt - _currentRun.Metadata.ExecutedAtUtc;
            var scenarioCount = _currentRun.Scenarios.Count;
            var runMode = runModeOverride ?? ResolveRunMode(scenarioCount);

            _currentRun = new SecurityRunSnapshot
            {
                Metadata = CopyMetadata(
                    _currentRun.Metadata,
                    runMode: runMode,
                    completedAtUtc: completedAt,
                    duration: duration),
                Scenarios = _currentRun.Scenarios
            };
            PersistLocked();
        }
    }

    public static bool TryRenderReport(out string? skipReason)
    {
        skipReason = null;

        if (!IsEnabled)
        {
            skipReason = "Security reporting is disabled.";
            return false;
        }

        if (ShouldDeferRender)
        {
            skipReason = "Report render deferred for parallel session merge.";
            return false;
        }

        lock (Sync)
        {
            if (_reportRendered)
            {
                skipReason = "Report already rendered for this run.";
                return false;
            }

            if (_currentRun == null || _currentRun.Scenarios.Count == 0)
            {
                skipReason = "No @Security/@Authorization scenarios in this run — report skipped.";
                return false;
            }

            _reportRendered = true;
        }

        return true;
    }

    public static SecurityRunSnapshot? LoadRun(string? runFilePath = null)
    {
        var path = runFilePath ?? CurrentRunFilePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SecurityRunSnapshot>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static IReadOnlyList<string> FindWorkerRunFiles(string sessionId)
    {
        var settings = SecurityReportingSettings;
        var runsDir = ResolvePath(settings.RunsPath);
        if (!Directory.Exists(runsDir))
            return [];

        return Directory
            .GetFiles(runsDir, $"run_{sessionId}_worker_*.json")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string ResolveProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (dir.GetFiles("EnterpriseApiSecurityAutomationFramework.csproj").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }

        dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (dir.GetFiles("*.csproj").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    public static string ResolvePath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
            return relativePath;

        return Path.Combine(ResolveProjectRoot(), relativePath);
    }

    public static string GenerateRunId()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        var suffix = Guid.NewGuid().ToString("N")[..4];
        return $"{timestamp}_{suffix}";
    }

    private static string BuildRunFilePath(string runsDir, string runId)
    {
        var sessionId = Environment.GetEnvironmentVariable(SecurityReportingConstants.SessionIdEnvVar);
        if (!string.IsNullOrWhiteSpace(sessionId))
            return Path.Combine(runsDir, $"run_{runId}_worker_{Environment.ProcessId}.json");

        return Path.Combine(runsDir, $"run_{runId}.json");
    }

    private static SecurityRunSnapshot CreateSnapshot(string runId, DateTimeOffset executedAtUtc) =>
        new()
        {
            Metadata = new SecurityRunMetadata
            {
                RunId = runId,
                ProjectName = "Enterprise API Security Automation Framework",
                Environment = AppConfiguration.EnvironmentName,
                ApiBaseUrl = AppConfiguration.ApiUrls.ApiBaseUrl,
                AuthBaseUrl = AppConfiguration.ApiUrls.AuthBaseUrl,
                Framework = "C# .NET 8 + Reqnroll + NUnit + RestSharp",
                ExecutedAtUtc = executedAtUtc,
                TestFilter = Environment.GetEnvironmentVariable(SecurityReportingConstants.TestFilterEnvVar)
            },
            Scenarios = new List<SecurityScenarioReport>()
        };

    private static SecurityRunSnapshot CreateFallbackRun()
    {
        var settings = SecurityReportingSettings;
        var runId = GenerateRunId();
        var runsDir = ResolvePath(settings.RunsPath);
        Directory.CreateDirectory(runsDir);
        _runFilePath = BuildRunFilePath(runsDir, runId);
        return CreateSnapshot(runId, DateTimeOffset.UtcNow);
    }

    private static string ResolveRunMode(int scenarioCount)
    {
        if (scenarioCount <= 1)
            return SecurityReportingConstants.RunModeSingleScenario;

        if (scenarioCount >= SecurityReportingConstants.FullSuiteScenarioThreshold)
            return SecurityReportingConstants.RunModeFullSuite;

        return SecurityReportingConstants.RunModeBatch;
    }

    private static SecurityRunMetadata CopyMetadata(
        SecurityRunMetadata source,
        string? trxFilePath = null,
        string? runMode = null,
        DateTimeOffset? completedAtUtc = null,
        TimeSpan? duration = null) =>
        new()
        {
            RunId = source.RunId,
            ProjectName = source.ProjectName,
            Environment = source.Environment,
            ApiBaseUrl = source.ApiBaseUrl,
            AuthBaseUrl = source.AuthBaseUrl,
            Framework = source.Framework,
            ExecutedAtUtc = source.ExecutedAtUtc,
            TrxFilePath = trxFilePath ?? source.TrxFilePath,
            RunMode = runMode ?? source.RunMode,
            TestFilter = source.TestFilter,
            CompletedAtUtc = completedAtUtc ?? source.CompletedAtUtc,
            Duration = duration ?? source.Duration,
            SkippedScenarios = source.SkippedScenarios
        };

    private static void PersistLocked()
    {
        if (_currentRun == null || string.IsNullOrWhiteSpace(_runFilePath))
            return;

        var json = JsonSerializer.Serialize(_currentRun, JsonOptions);
        File.WriteAllText(_runFilePath, json);
    }
}
