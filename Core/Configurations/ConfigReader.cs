namespace EnterpriseApiAutomationFramework.Core.Configurations;

/// <summary>
/// Backward-compatible accessor. Prefer <see cref="AppConfiguration"/> for new code.
/// </summary>
public static class ConfigReader
{
    public static string GetValue(string key) => AppConfiguration.GetValue(key);
}
