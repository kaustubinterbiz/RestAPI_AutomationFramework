using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Models.Response;

namespace EnterpriseApiAutomationFramework.Core.Authentication;

/// <summary>
/// Parses GetSessionInfo API response and  persists values to appsettings.json (and endpoint id cache).
/// </summary>
public static class SessionInfoStore
{
    public const string AppSettingsFile = "appsettings.json";
    public const string SessionInfoSection = "SessionInfo";

    /// <summary>
    /// Parses response, saves all properties under SessionInfo in appsettings.json,
    /// and updates flat keys (CacheId, id) for later API calls.
    /// </summary>
    public static GetSessionInfo SaveFromResponse(
        string? responseContent,
        string appSettingsFile = AppSettingsFile,
        bool updateEndpointId = true)
    {
        var sessionInfo = GetSessionInfoResponseParse.TryGetSessionInfo(responseContent)
            ?? throw new InvalidOperationException(
                "Could not parse GetSessionInfo from the last API response.");

        var properties = GetSessionInfoResponseParse.ToPropertyDictionary(sessionInfo);
        ConfigReaderNew.UpdateJsonSection(appSettingsFile, SessionInfoSection, properties);

        foreach (var (key, value) in properties)
        {
            ConfigReaderNew.UpdateJsonValue(appSettingsFile, key, value);
        }

        if (updateEndpointId && !string.IsNullOrWhiteSpace(sessionInfo.CacheId))
        {
            UpdateEndpointCachedId(sessionInfo.CacheId, appSettingsFile);
        }

        return sessionInfo;
    }

    public static string? GetCachedValue(string key, string appSettingsFile = AppSettingsFile)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        ConfigReaderNew.LoadConfig(appSettingsFile);

        var sectionValue = ConfigReaderNew.GetJsonSectionValue(
            appSettingsFile,
            SessionInfoSection,
            key);

        if (!string.IsNullOrWhiteSpace(sectionValue))
        {
            return sectionValue;
        }

        var flatValue = ConfigReaderNew.GetJsonValue(appSettingsFile, key);
        return string.IsNullOrWhiteSpace(flatValue) ? null : flatValue;
    }

    private static void UpdateEndpointCachedId(string cacheId, string appSettingsFile)
    {
        ConfigReaderNew.LoadConfig(appSettingsFile);
        var endpointFile = ConfigReaderNew.GetValue("EndpointJson");

        if (string.IsNullOrWhiteSpace(endpointFile))
        {
            return;
        }

        ConfigReaderNew.UpdateJsonValue(endpointFile, "id", cacheId);
        ConfigReaderNew.UpdateJsonValue(endpointFile, "CacheId", cacheId);
    }
}
