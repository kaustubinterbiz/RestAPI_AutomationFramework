using System.Security.Cryptography;
using System.Text;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Shared read access for Excel workbooks under TestData/UploadFiles (tolerates Excel/WPS open handles).
/// </summary>
internal static class WorkbookFileAccess
{
    private static readonly string UploadSnapshotRoot = Path.Combine(
        Path.GetTempPath(),
        "RestAPI_AutomationFramework",
        "uploads");

    /// <summary>Reads the whole file into memory with sharing enabled, then closes the handle.</summary>
    public static byte[] ReadAllBytesAllowSharing(string fullPath)
    {
        using var fs = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var ms = new MemoryStream();
        fs.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Copies workbook bytes to a temp upload snapshot. Returns null when read/write fails.
    /// </summary>
    public static string? TryCreateUploadSnapshot(string sourceFullPath)
    {
        try
        {
            var bytes = ReadAllBytesAllowSharing(sourceFullPath);
            Directory.CreateDirectory(UploadSnapshotRoot);

            var fileName = Path.GetFileName(sourceFullPath);
            var hash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(sourceFullPath.ToLowerInvariant())))[..16];
            var destPath = Path.Combine(UploadSnapshotRoot, $"{hash}_{fileName}");

            File.WriteAllBytes(destPath, bytes);
            return destPath;
        }
        catch
        {
            return null;
        }
    }
}
