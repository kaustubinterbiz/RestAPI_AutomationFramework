using System.Collections.Concurrent;
using System.Diagnostics;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Detects Office/Excel lock files (~$*.xlsx) and emits a one-time warning per workbook per process.
/// </summary>
internal static class ExcelOpenFileDetector
{
    private static readonly ConcurrentDictionary<string, byte> WarnedWorkbooks = new(StringComparer.OrdinalIgnoreCase);

    private const string WarningMessage =
        "Excel appears open for '{0}'; tests use last saved disk content — save (Ctrl+S) before relying on unsaved edits.";

    public static void WarnIfWorkbookPossiblyOpen(string workbookFullPath)
    {
        if (!TryGetLockFilePath(workbookFullPath, out var lockPath) || !File.Exists(lockPath))
            return;

        if (!WarnedWorkbooks.TryAdd(workbookFullPath, 0))
            return;

        var message = string.Format(WarningMessage, workbookFullPath);
        Debug.WriteLine(message);

        try
        {
            NUnit.Framework.TestContext.Progress.WriteLine(message);
        }
        catch
        {
            // Outside NUnit test host — Debug.WriteLine is enough.
        }
    }

    /// <summary>Office creates ~$OriginalName.xlsx alongside the workbook when it is open for editing.</summary>
    internal static bool TryGetLockFilePath(string workbookFullPath, out string lockFilePath)
    {
        lockFilePath = string.Empty;
        if (string.IsNullOrWhiteSpace(workbookFullPath))
            return false;

        var directory = Path.GetDirectoryName(workbookFullPath);
        var fileName = Path.GetFileName(workbookFullPath);
        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
            return false;

        lockFilePath = Path.Combine(directory, "~$" + fileName);
        return true;
    }
}
