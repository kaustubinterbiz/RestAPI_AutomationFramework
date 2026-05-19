using Microsoft.Extensions.Configuration;

namespace EnterpriseApiAutomationFramework.Core.Configurations;

public static class ConfigReader
{
    private static readonly IConfigurationRoot configuration;

    static ConfigReader()
    {
        configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
    }

    
    public static string GetValue(string key)
    {
        return configuration[key] ?? string.Empty;
    }
}