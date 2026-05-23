using Microsoft.Extensions.Configuration;

namespace EnterpriseApiAutomationFramework.Core.Configurations;

/// <summary>
/// Layered configuration: appsettings.json + appsettings.{Environment}.json + environment variables.
/// Environment name: TEST_ENVIRONMENT variable, then "Environment" key, default "QA".
/// </summary>
public static class AppConfiguration
{
    private static readonly object Sync = new();
    private static IConfigurationRoot? _configuration;
    private static ApiEnvironmentSettings? _apiSettings;

    public static IConfigurationRoot Instance
    {
        get
        {
            lock (Sync)
            {
                return _configuration ??= BuildConfiguration();
            }
        }
    }

    public static string EnvironmentName =>
        Environment.GetEnvironmentVariable("TEST_ENVIRONMENT")
        ?? Instance["Environment"]
        ?? "QA";

    public static ApiEnvironmentSettings ApiUrls
    {
        get
        {
            lock (Sync)
            {
                if (_apiSettings is not null)
                {
                    return _apiSettings;
                }

                _apiSettings = new ApiEnvironmentSettings();
                Instance.GetSection("ApiUrls").Bind(_apiSettings);

                if (_apiSettings.TimeoutMilliseconds <= 0)
                {
                    var timeout = Instance["Timeout"];
                    if (int.TryParse(timeout, out var ms) && ms > 0)
                    {
                        _apiSettings.TimeoutMilliseconds = ms;
                    }
                }

                return _apiSettings;
            }
        }
    }

    public static string GetValue(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return key switch
        {
            "BaseUrl" => ApiUrls.ApiBaseUrl,
            "AuthBaseUrl" => ApiUrls.AuthBaseUrl,
            "ApiBaseUrl" => ApiUrls.ApiBaseUrl,
            _ => Instance[key] ?? string.Empty
        };
    }

    public static bool GetBool(string key, bool defaultValue = false)
    {
        var value = Instance[key];
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    public static void Reload()
    {
        lock (Sync)
        {
            _configuration = null;
            _apiSettings = null;
        }
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var basePath = Directory.GetCurrentDirectory();
        var baseConfig = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var environment =
            Environment.GetEnvironmentVariable("TEST_ENVIRONMENT")
            ?? baseConfig["Environment"]
            ?? "QA";

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables(prefix: "ROVI_")
            .Build();
    }
}
