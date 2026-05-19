using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EnterpriseApiAutomationFramework.Core.Configurations;

public static class ConfigReaderNew
{
    // Dictionary to store multiple config files
    private static readonly Dictionary<string, IConfigurationRoot> _configs = new();

    /// <summary>
    /// Load any JSON config file dynamically with a key name
    /// Example: "app", "login", "endpoint"
    /// </summary>
    public static void LoadConfig(
        string configName,
        string fileName = "appsettings.json")
    {
        string fullPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            fileName);

        Console.WriteLine($"Loading config: {fullPath}");

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Configuration file not found: {fullPath}");
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile(fileName, optional: false, reloadOnChange: true)
            .Build();

        _configs[configName] = configuration;
    }

    /// <summary>
    /// Get value from specific config file using key
    /// </summary>
    public static string GetValue(string configName, string key)
    {
        if (!_configs.ContainsKey(configName))
        {
            throw new Exception(
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
    /// Get specific value directly from JSON file
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