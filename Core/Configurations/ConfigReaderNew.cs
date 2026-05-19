using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace EnterpriseApiAutomationFramework.Core.Configurations;

/// <summary>
/// Thread-safe, file-based configuration loader.
/// Each <see cref="LoadConfig(string)"/> call disposes prior data and loads only the new file.
/// Use <see cref="GetValue(string)"/> on this class — not <see cref="ConfigReader"/> — after loading.
/// </summary>
public static class ConfigReaderNew
{
    public const string DefaultConfigKey = "default";

    private static readonly object Sync = new();
    private static readonly Dictionary<string, ConfigurationEntry> Entries =
        new(StringComparer.OrdinalIgnoreCase);

    private static string _activeConfigKey = DefaultConfigKey;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    /// <summary>
    /// True when at least one configuration has been loaded.
    /// </summary>
    public static bool IsLoaded
    {
        get
        {
            lock (Sync)
            {
                return Entries.Count > 0;
            }
        }
    }

    /// <summary>
    /// Loads a JSON file as the sole active configuration.
    /// All previously loaded entries are disposed and removed.
    /// </summary>
    public static void LoadConfig(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var resolvedPath = ResolvePath(fileName);
        var entry = CreateEntry(resolvedPath);

        lock (Sync)
        {
            DisposeAllEntries();
            Entries[DefaultConfigKey] = entry;
            _activeConfigKey = DefaultConfigKey;
        }
    }

    /// <summary>
    /// Loads (or replaces) a named configuration without clearing other named entries.
    /// </summary>
    public static void LoadConfig(
        string configName,
        string fileName,
        bool setAsActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var resolvedPath = ResolvePath(fileName);
        var entry = CreateEntry(resolvedPath);

        lock (Sync)
        {
            if (Entries.Remove(configName, out var previous))
            {
                previous.Dispose();
            }

            Entries[configName] = entry;

            if (setAsActive)
            {
                _activeConfigKey = configName;
            }
        }
    }

    /// <summary>
    /// Sets which named configuration <see cref="GetValue(string)"/> reads from.
    /// </summary>
    public static void SetActiveConfig(string configName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configName);

        lock (Sync)
        {
            if (!Entries.ContainsKey(configName))
            {
                throw new InvalidOperationException(
                    $"Config '{configName}' is not loaded. Call LoadConfig first.");
            }

            _activeConfigKey = configName;
        }
    }

    /// <summary>
    /// Gets a value from the active configuration (nested keys use ':' e.g. "App:BaseUrl").
    /// </summary>
    public static string GetValue(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        lock (Sync)
        {
            return GetValueCore(_activeConfigKey, key);
        }
    }

    /// <summary>
    /// Gets a value from a named configuration.
    /// </summary>
    public static string GetValue(string configName, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        lock (Sync)
        {
            return GetValueCore(configName, key);
        }
    }

    /// <summary>
    /// Binds a configuration section to a strongly-typed object from the active config.
    /// </summary>
    public static T GetSection<T>(string sectionName) where T : class, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        lock (Sync)
        {
            return GetSectionCore<T>(_activeConfigKey, sectionName);
        }
    }

    /// <summary>
    /// Binds a configuration section from a named config to a strongly-typed object.
    /// </summary>
    public static T GetSection<T>(string configName, string sectionName)
        where T : class, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        lock (Sync)
        {
            return GetSectionCore<T>(configName, sectionName);
        }
    }

    /// <summary>
    /// Removes and disposes a single named configuration.
    /// </summary>
    public static bool Unload(string configName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configName);

        lock (Sync)
        {
            if (!Entries.Remove(configName, out var entry))
            {
                return false;
            }

            entry.Dispose();

            if (_activeConfigKey.Equals(configName, StringComparison.OrdinalIgnoreCase))
            {
                _activeConfigKey = Entries.Keys.FirstOrDefault() ?? DefaultConfigKey;
            }

            return true;
        }
    }

    /// <summary>
    /// Disposes all loaded configurations and clears in-memory state.
    /// </summary>
    public static void ClearAll()
    {
        lock (Sync)
        {
            DisposeAllEntries();
            _activeConfigKey = DefaultConfigKey;
        }
    }

    /// <summary>
    /// Deserializes a JSON file into <typeparamref name="T"/> (always reads from disk; not cached).
    /// </summary>
    public static T ReadJson<T>(string filePath)
    {
        var json = ReadFileText(filePath);
        var result = JsonSerializer.Deserialize<T>(json, JsonOptions);

        return result ?? throw new JsonException(
            $"Failed to deserialize '{filePath}' to {typeof(T).Name}.");
     }

    /// <summary>
    /// Reads a JSON file as a mutable <see cref="JsonNode"/> tree (always reads from disk).
    /// </summary>
    public static JsonNode ReadJson(string filePath)
    {
        var json = ReadFileText(filePath);
        return JsonNode.Parse(json, documentOptions: new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        }) ?? throw new JsonException($"JSON file '{filePath}' is empty or invalid.");
    }

    /// <summary>
    /// Serializes an object to a JSON file.
    /// </summary>
    public static void WriteJson<T>(string filePath, T data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var fullPath = ResolvePath(filePath);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(fullPath, json);
    }

    /// <summary>
    /// Updates a top-level key in a JSON file (flat keys only).
    /// </summary>
    public static void UpdateJsonValue(string filePath, string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var fullPath = ResolvePath(filePath);
        var jsonNode = ReadJson(filePath);

        if (jsonNode is not JsonObject jsonObject)
        {
            throw new JsonException($"JSON root in '{fullPath}' is not an object.");
        }

        jsonObject[key] = value;
        File.WriteAllText(fullPath, jsonObject.ToJsonString(JsonOptions));
    }

    /// <summary>
    /// Reads a top-level value directly from a JSON file (not from loaded config cache).
    /// </summary>
    public static string GetJsonValue(string filePath, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var jsonObject = ReadJson(filePath);

        if (jsonObject is not JsonObject obj)
        {
            throw new JsonException($"JSON root in '{filePath}' is not an object.");
        }

        return obj[key]?.ToString() ?? string.Empty;
    }

    private static string GetValueCore(string configName, string key)
    {
        if (!Entries.TryGetValue(configName, out var entry))
        {
            throw new InvalidOperationException(
                $"Config '{configName}' is not loaded. Call LoadConfig first.");
        }

        return entry.Root[key] ?? string.Empty;
    }

    private static T GetSectionCore<T>(string configName, string sectionName)
        where T : class, new()
    {
        if (!Entries.TryGetValue(configName, out var entry))
        {
            throw new InvalidOperationException(
                $"Config '{configName}' is not loaded. Call LoadConfig first.");
        }

        var section = entry.Root.GetSection(sectionName);
        var result = section.Get<T>();

        return result ?? throw new InvalidOperationException(
            $"Section '{sectionName}' was not found or could not be bound in config '{configName}'.");
    }

    private static ConfigurationEntry CreateEntry(string resolvedPath)
    {
        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException(
                $"Configuration file not found: {resolvedPath}",
                resolvedPath);
        }

        // New builder per load — no shared providers, reloadOnChange: false avoids file watchers.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(resolvedPath)!)
            .AddJsonFile(Path.GetFileName(resolvedPath), optional: false, reloadOnChange: false)
            .Build();

        return new ConfigurationEntry(configuration, resolvedPath);
    }

    private static void DisposeAllEntries()
    {
        foreach (var entry in Entries.Values)
        {
            entry.Dispose();
        }

        Entries.Clear();
    }

    private static string ResolvePath(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (Path.IsPathRooted(filePath))
        {
            return Path.GetFullPath(filePath);
        }

        // Prefer files next to the .csproj (source tree) so appsettings Token
        // updates are visible in the IDE, not only under bin\Debug\...\.
        var nearProject = FindFileNearProjectRoot(filePath);
        if (nearProject != null)
        {
            return nearProject;
        }

        var inOutput = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, filePath));
        if (File.Exists(inOutput))
        {
            return inOutput;
        }

        var inWorkingDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), filePath));
        return inWorkingDir;
    }

    private static string? FindFileNearProjectRoot(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            if (dir.GetFiles("*.csproj").Length > 0)
            {
                var candidate = Path.GetFullPath(Path.Combine(dir.FullName, relativePath));
                return File.Exists(candidate) ? candidate : null;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static string ReadFileText(string filePath)
    {
        var fullPath = ResolvePath(filePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"JSON file not found: {fullPath}", fullPath);
        }

        return File.ReadAllText(fullPath);
    }

    private sealed class ConfigurationEntry(IConfigurationRoot root, string resolvedPath) : IDisposable
    {
        public IConfigurationRoot Root { get; } =
            root ?? throw new ArgumentNullException(nameof(root));

        public string ResolvedPath { get; } =
            resolvedPath ?? throw new ArgumentNullException(nameof(resolvedPath));

        public void Dispose()
        {
            if (Root is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
