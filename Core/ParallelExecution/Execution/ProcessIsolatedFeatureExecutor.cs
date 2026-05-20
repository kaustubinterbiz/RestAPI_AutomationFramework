using System.Diagnostics;
using System.Text;
using EnterpriseApiAutomationFramework.Core.ParallelExecution.Configuration;
using EnterpriseApiAutomationFramework.Core.ParallelExecution.Models;
using EnterpriseApiAutomationFramework.Core.ParallelExecution.Reporting;

namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Execution;

/// <summary>
/// Runs each execution unit in an isolated dotnet test OS process for true concurrency.
/// </summary>
public sealed class ProcessIsolatedFeatureExecutor
{
    private readonly ParallelExecutionSettings _settings;
    private readonly string _projectPath;
    private readonly string _projectRoot;

    public ProcessIsolatedFeatureExecutor(
        ParallelExecutionSettings settings,
        string projectPath,
        string projectRoot)
    {
        _settings = settings;
        _projectPath = projectPath;
        _projectRoot = projectRoot;
    }

    public Task<FeatureExecutionResult> ExecuteAsync(
        ExecutionUnitDescriptor unit,
        int attemptNumber,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => ExecuteCoreAsync(unit, attemptNumber, cancellationToken), cancellationToken);

    private async Task<FeatureExecutionResult> ExecuteCoreAsync(
        ExecutionUnitDescriptor unit,
        int attemptNumber,
        CancellationToken cancellationToken)
    {
        var startUtc = DateTime.UtcNow;
        var workerDir = Path.Combine(
            ResolvePath(_settings.WorkerOutputPath),
            $"{SanitizeId(unit.Id)}_{startUtc:yyyyMMdd_HHmmss_fff}");

        Directory.CreateDirectory(workerDir);

        var trxPath = Path.Combine(workerDir, "results.trx");
        var logPath = Path.Combine(workerDir, "execution.log");
        var extentDir = Path.Combine(workerDir, "Extent");
        Directory.CreateDirectory(extentDir);

        var logBuilder = new StringBuilder();
        logBuilder.AppendLine($"[{DateTime.UtcNow:O}] [PID {Environment.ProcessId}] Starting '{unit.ModuleName}' ({unit.UnitType}) attempt {attemptNumber}");
        logBuilder.AppendLine($"Parent Feature: {unit.ParentFeatureName ?? "-"}");
        logBuilder.AppendLine($"Filter: {unit.TestFilterExpression}");

        var arguments =
            $"test \"{_projectPath}\" " +
            $"--filter \"{unit.TestFilterExpression}\" " +
            $"--logger \"trx;LogFileName={trxPath}\" " +
            $"--results-directory \"{workerDir}\" " +
            "--no-build " +
            "--nologo";

        var (exitCode, workerPid) = await RunProcessAsync(_projectRoot, arguments, extentDir, logBuilder, cancellationToken);
        await File.WriteAllTextAsync(logPath, logBuilder.ToString(), cancellationToken);

        var endUtc = DateTime.UtcNow;
        var scenarios = TrxResultParser.Parse(File.Exists(trxPath) ? trxPath : FindTrxFile(workerDir));
        var extentReport = Directory.GetFiles(extentDir, "*.html").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();

        var status = exitCode == 0 && (scenarios.Count == 0 || scenarios.All(s => s.Status == ExecutionStatus.Success))
            ? ExecutionStatus.Success
            : ExecutionStatus.Failed;

        var errorSummary = status == ExecutionStatus.Success
            ? null
            : BuildErrorSummary(exitCode, scenarios);

        return new FeatureExecutionResult
        {
            ModuleId = unit.Id,
            ModuleName = unit.ModuleName,
            UnitType = unit.UnitType,
            ParentFeatureName = unit.ParentFeatureName,
            Status = status,
            StartTimeUtc = startUtc,
            EndTimeUtc = endUtc,
            AttemptCount = attemptNumber,
            ExitCode = exitCode,
            WorkerOsProcessId = workerPid,
            ErrorSummary = errorSummary,
            WorkerOutputDirectory = workerDir,
            TrxFilePath = File.Exists(trxPath) ? trxPath : FindTrxFile(workerDir),
            LogFilePath = logPath,
            ExtentReportPath = extentReport,
            Scenarios = scenarios,
            Logs = logBuilder.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
        };
    }

    private static async Task<(int ExitCode, int? WorkerPid)> RunProcessAsync(
        string projectRoot,
        string arguments,
        string extentReportDir,
        StringBuilder logBuilder,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = projectRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.Environment["PARALLEL_WORKER_REPORT_PATH"] = extentReportDir;
        psi.Environment["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1";
        psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        psi.Environment["MSBUILDNOINPROCNODE"] = "1";

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var outputLock = new object();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            lock (outputLock) logBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            lock (outputLock) logBuilder.AppendLine($"[stderr] {e.Data}");
        };

        logBuilder.AppendLine($"[{DateTime.UtcNow:O}] Spawning worker process: dotnet {arguments}");

        process.Start();
        logBuilder.AppendLine($"[{DateTime.UtcNow:O}] Worker OS PID: {process.Id}");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        var pid = process.Id;
        logBuilder.AppendLine($"[{DateTime.UtcNow:O}] Worker exited with code {process.ExitCode}");
        return (process.ExitCode, pid);
    }

    private static string SanitizeId(string id)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            id = id.Replace(c, '_');
        return id.Replace(' ', '_');
    }

    private static string? FindTrxFile(string directory) =>
        Directory.GetFiles(directory, "*.trx", SearchOption.AllDirectories).FirstOrDefault();

    private static string BuildErrorSummary(int exitCode, IReadOnlyList<ScenarioExecutionResult> scenarios)
    {
        var failed = scenarios.Where(s => s.Status == ExecutionStatus.Failed).ToList();
        if (failed.Count > 0)
            return $"Failed scenarios: {failed.Count}. First error: {failed[0].ErrorMessage}";

        return exitCode == 0 ? "Unknown failure." : $"Process exited with code {exitCode}.";
    }

    private string ResolvePath(string relativePath) =>
        Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.GetFullPath(Path.Combine(_projectRoot, relativePath));
}
