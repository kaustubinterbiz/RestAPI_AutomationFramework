using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EnterpriseApiAutomationFramework.Core.Configurations;

public static class ConfigReaderNew
{
    private static readonly Dictionary<string, IConfigurationRoot> _configs = new();
    private static string? _activeConfigName;

    /// <summary>
    /// Load a JSON file and set it as the active config (keys from GetValue come from this file).
    /// </summary>
    public static void LoadConfig(string fileName)
    {
        LoadConfig(fileName, fileName, setAsActive: true);
    }

    /// <summary>
    /// Load any JSON config file with a logical name. When setAsActive is true (default),
    /// subsequent GetValue(key) calls read from this file, not a previously loaded one.
    /// </summary>
    public static void LoadConfig(
        string configName,
        string fileName,
        bool setAsActive = true)
    {
        string fullPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            fileName);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Configuration file not found: {fullPath}");
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile(fileName, optional: false, reloadOnChange: false)
            .Build();

        _configs[configName] = configuration;

        if (setAsActive)
        {
            _activeConfigName = configName;
        }
    }

    /// <summary>
    /// Get a value from the currently active (most recently loaded) config file.
    /// </summary>
    public static string GetValue(string key)
    {
        if (string.IsNullOrEmpty(_activeConfigName) || !_configs.ContainsKey(_activeConfigName))
        {
            throw new InvalidOperationException(
                "No config is active. Call LoadConfig first.");
        }

        return _configs[_activeConfigName][key] ?? string.Empty;
    }

    /// <summary>
    /// Get a value from a specific named config (does not change the active config).
    /// </summary>
    public static string GetValue(string configName, string key)
    {
        if (!_configs.ContainsKey(configName))
        {
            throw new InvalidOperationException(
                $"Config '{configName}' is not loaded. Call LoadConfig first.");
        }

        return _configs[configName][key] ?? string.Empty;
    }

    /// <summary>
    /// Read complete JSON file and deserialize into object
    /// </summary>
    public static T ReadJson<T>(string filePath)
    {
        string fullPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            filePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"JSON file not found: {fullPath}");
        }

        var json = File.ReadAllText(fullPath);

        return JsonConvert.DeserializeObject<T>(json)!;
    }

    /// <summary>
    /// Read JSON as JObject
    /// </summary>
    public static JObject ReadJson(string filePath)
    {
        string fullPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            filePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"JSON file not found: {fullPath}");
        }

        var json = File.ReadAllText(fullPath);

        return JObject.Parse(json);
    }

    /// <summary>
    /// Write object into JSON file
    /// </summary>
    public static void WriteJson<T>(string filePath, T data)
    {
        string fullPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            filePath);

        var json = JsonConvert.SerializeObject(
            data,
            Formatting.Indented);

        File.WriteAllText(fullPath, json);
    }

    /// <summary>
    /// Update JSON key value (flat JSON only)
    /// </summary>
    public static void UpdateJsonValue(
        string filePath,
        string key,
        string value)
    {
        string fullPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            filePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"JSON file not found: {fullPath}");
        }

        var json = File.ReadAllText(fullPath);

        var jObject = JObject.Parse(json);

        jObject[key] = value;

        File.WriteAllText(
            fullPath,
            jObject.ToString(Formatting.Indented));
    }

    /// <summary>
    /// Get specific value directly from JSON file (one-off read, no LoadConfig required)
    /// </summary>
    public static string GetJsonValue(
        string filePath,
        string key)
    {
        string fullPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            filePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"JSON file not found: {fullPath}");
        }

        var json = File.ReadAllText(fullPath);

        var jObject = JObject.Parse(json);

        return jObject[key]?.ToString() ?? string.Empty;
    }
}
