using EnterpriseApiAutomationFramework.Core.ParallelExecution.Configuration;
using EnterpriseApiAutomationFramework.Core.ParallelExecution.Execution;

namespace EnterpriseApiAutomationFramework.ParallelTestRunner;

/// <summary>
/// Standalone host for true parallel execution (runs outside NUnit test host to avoid DLL locks).
/// </summary>
internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var projectRoot = ResolveProjectRoot();
            var projectPath = Path.Combine(projectRoot, "EnterpriseApiAutomationFramework.csproj");
            var settings = ParallelExecutionSettings.Load();

            Console.WriteLine("=== Parallel Test Runner (out-of-process) ===");
            Console.WriteLine($"Project : {projectPath}");
            Console.WriteLine($"Mode    : {settings.Granularity}");
            Console.WriteLine();

            var orchestrator = new ParallelOrchestrator(settings, projectPath, projectRoot);
            var report = await orchestrator.RunAsync();

            return report.FailedModules > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Parallel runner failed: {ex.Message}");
            Console.Error.WriteLine(ex);
            return 2;
        }
    }

    private static string ResolveProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "EnterpriseApiAutomationFramework.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
