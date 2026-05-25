using System.Text.Json;
using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Authentication;

/// <summary>
/// File-backed token cache with mutex for parallel / multi-process workers.
/// </summary>
internal static class TokenCacheStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static bool TryLoadValid(
        string cacheFilePath,
        string loginRoleKey,
        string environment,
        int refreshBufferMinutes,
        out CachedTokenEntry entry)
    {
        entry = null!;

        var fullPath = ConfigReaderNew.ResolvePathForRead(cacheFilePath);
        if (!File.Exists(fullPath))
        {
            return false;
        }

        try
        {
            using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            var cached = JsonSerializer.Deserialize<CachedTokenEntry>(stream, JsonOptions);
            if (cached is null
                || string.IsNullOrWhiteSpace(cached.AccessToken)
                || !string.Equals(cached.LoginRoleKey, loginRoleKey, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(cached.Environment, environment, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!IsStillValid(cached.ExpiresAtUtc, refreshBufferMinutes))
            {
                return false;
            }

            entry = cached;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void Save(string cacheFilePath, CachedTokenEntry entry)
    {
        var fullPath = ConfigReaderNew.ResolvePathForWrite(cacheFilePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var mutexName = $"Global\\RoviTokenCache_{fullPath.GetHashCode():X}";
        using var mutex = new Mutex(false, mutexName);

        if (!mutex.WaitOne(TimeSpan.FromSeconds(30)))
        {
            throw new TimeoutException($"Timed out waiting to write token cache: {fullPath}");
        }

        try
        {
            var tempPath = fullPath + ".tmp";
            var json = JsonSerializer.Serialize(entry, JsonOptions);
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, fullPath, overwrite: true);
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    private static bool IsStillValid(DateTimeOffset expiresAtUtc, int refreshBufferMinutes) =>
        expiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(refreshBufferMinutes);
}
