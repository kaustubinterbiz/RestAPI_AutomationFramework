using Microsoft.Extensions.Configuration;

namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Configuration;

public enum ParallelGranularity
{
    Feature,
    Scenario
}

public sealed class ParallelExecutionSettings
{
    public bool Enabled { get; init; } = true;
    /// <summary>
    /// Feature = one worker per .feature file. Scenario = one worker per scenario (true max parallelism).
    /// </summary>
    public ParallelGranularity Granularity { get; init; } = ParallelGranularity.Scenario;
    /// <summary>
    /// Max concurrent workers. 0 = run all discovered units at once.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 0;
    public int RetryCount { get; init; } = 1;
    public int RetryDelayMilliseconds { get; init; } = 2000;
    public string ConsolidatedReportPath { get; init; } = "Reports/Parallel/Consolidated";
    public string WorkerOutputPath { get; init; } = "Reports/Parallel/Workers";
    public bool FailFast { get; init; }

    public static ParallelExecutionSettings Load(IConfiguration? configuration = null)
    {
        configuration ??= new ConfigurationBuilder()
            .SetBasePath(ResolveProjectRoot())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var section = configuration.GetSection("ParallelExecution");
        return section.Get<ParallelExecutionSettings>() ?? new ParallelExecutionSettings();
    }

    private static string ResolveProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (dir.GetFiles("*.csproj").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
