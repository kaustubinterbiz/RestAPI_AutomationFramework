namespace EnterpriseApiAutomationFramework.Core.Authentication;

using EnterpriseApiAutomationFramework.Core.Configurations;

public static class TokenManager
{
    public static string AccessToken { get; private set; } = string.Empty;

    public static void InitializeFromConfig(string appSettingsFile = "appsettings.json")
    {
        AccessToken = ConfigReaderNew.GetJsonValue(appSettingsFile, "Token");
    }

    public static void SetAccessToken(string token, string appSettingsFile = "appsettings.json")
    {
        AccessToken = token;
        ConfigReaderNew.UpdateJsonValue(appSettingsFile, "Token", token);
    }
}