using System.Diagnostics;
using EnterpriseApiAutomationFramework.Core.ParallelExecution.Configuration;
using NUnit.Framework;

namespace EnterpriseApiAutomationFramework.ParallelExecution;

/// <summary>
/// Launches the out-of-process parallel runner so workers are not serialized by the NUnit test host.
/// </summary>
[TestFixture]
[Category("ParallelOrchestrator")]
public sealed class ParallelOrchestratorTests
{
    [Test]
    [Explicit("Runs parallel execution via out-of-process ParallelTestRunner host.")]
    public async Task RunAllModulesInParallel_WithConsolidatedReport()
    {
        var projectRoot = ResolveProjectRoot();
        var runnerProject = Path.Combine(projectRoot, "ParallelTestRunner", "ParallelTestRunner.csproj");

        var exitCode = await RunParallelHostAsync(runnerProject, projectRoot);

        Assert.That(exitCode, Is.EqualTo(0),
            "Parallel run failed. See consolidated report under Reports/Parallel/Consolidated/.");
    }

    private static async Task<int> RunParallelHostAsync(string runnerProject, string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{runnerProject}\" --nologo",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start ParallelTestRunner.");

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        TestContext.WriteLine(stdout);
        if (!string.IsNullOrWhiteSpace(stderr))
            TestContext.WriteLine(stderr);

        return process.ExitCode;
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
