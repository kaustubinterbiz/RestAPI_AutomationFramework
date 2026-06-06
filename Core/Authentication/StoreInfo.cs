using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseApiAutomationFramework.Core.Authentication
{
    public class StoreInfo
    {
        public const string AppSettingsFile = "appsettings.json";
        public const string SessionInfoSection = "SessionInfo";
        public const string CheckExistingUserInfoSection = "CheckExistingUserAvailabilityInfo";

        //Session Info 
        public static GetSessionInfo SaveSessionInfoFromResponse(
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
                UpdateResponseValuesInJsonFile(appSettingsFile, "EndpointJson", "CacheId", sessionInfo.CacheId);
            }

            return sessionInfo;
        }

        //Existing User
        public static CheckExistingUser_ResponseModel SaveExistingUserFromResponse(
        string? responseContent,
        string appSettingsFile = AppSettingsFile,
        bool updateEndpointId = true)
        {
            var existingUserInfo = InfoResponseParse.TryCheckExistingUserInfo(responseContent)
                ?? throw new InvalidOperationException(
                    "Could not parse CheckExistingUserInfo from the last API response.");

            var properties = InfoResponseParse.ToCheckExistingUserPropertyDictionary(existingUserInfo);
            ConfigReaderNew.UpdateJsonSection(appSettingsFile, CheckExistingUserInfoSection, properties);

            foreach (var (key, value) in properties)
            {
                ConfigReaderNew.UpdateJsonValue(appSettingsFile, key, value);
            }

            if (updateEndpointId && !string.IsNullOrWhiteSpace(existingUserInfo.MemberId))
            {
                UpdateResponseValuesInJsonFile(appSettingsFile, "EndpointJson", "MemberId", existingUserInfo.MemberId);
            }

            return existingUserInfo;
        }

        //Dynamic Response Handler
        private static void UpdateResponseValuesInJsonFile(string jsonFilePath, string Jsonkey, string updateOnKey, string storedValue)
        {
            ConfigReaderNew.LoadConfig(jsonFilePath);
            var targetFilePath = ConfigReaderNew.GetValue(Jsonkey);

            if (string.IsNullOrWhiteSpace(targetFilePath))
            {
                return;
            }

            ConfigReaderNew.UpdateJsonValue(targetFilePath, updateOnKey, storedValue);
        }

        //GetSessionIno and CacheId
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

        //GetExistingUserInfo and MemberId
        public static string? GetMemberIdValue(string key, string appSettingsFile = AppSettingsFile)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            ConfigReaderNew.LoadConfig(appSettingsFile);

            var existingUserValue = ConfigReaderNew.GetJsonSectionValue(
                appSettingsFile,
                CheckExistingUserInfoSection,
                key);

            if (!string.IsNullOrWhiteSpace(existingUserValue))
            {
                return existingUserValue;
            }

            var flatValue = ConfigReaderNew.GetJsonValue(appSettingsFile, key);
            return string.IsNullOrWhiteSpace(flatValue) ? null : flatValue;
        }
    }
}
