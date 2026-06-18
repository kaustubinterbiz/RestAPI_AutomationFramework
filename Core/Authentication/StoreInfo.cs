using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Models.Response;

namespace EnterpriseApiAutomationFramework.Core.Authentication
{
    public static class StoreInfo
    {
        public const string AppSettingsFile = "appsettings.json";
        public const string SessionInfoSection = "SessionInfo";
        public const string CheckExistingUserInfoSection = "CheckExistingUserAvailabilityInfo";
        public const string AddMultipleMemberByExcelInfoSection = "AddMultipleMemberByExcelInfo";

        //Session Info 
        public static GetSessionInfo SaveSessionInfoFromResponse(
        string? responseContent,
        string appSettingsFile = AppSettingsFile,
        bool updateEndpointId = true)
        {
            var sessionInfo = InfoResponseParse.TryGetSessionInfo(responseContent)
                ?? throw new InvalidOperationException(
                    "Could not parse GetSessionInfo from the last API response.");

            var properties = InfoResponseParse.ToGetSessionPropertyDictionary(sessionInfo);
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

        public static IList<AddMultipleMemberByExcel_ResponseModel>? SaveAddMultiMemberByExcelResponseToExcel(
            string? responseContent,
            string excelFileName = AddMultipleMemberByExcelDefaults.FileName,
            string responseSheetName = AddMultipleMemberByExcelDefaults.ResponseSheetName,
            IReadOnlyList<string>? columnNames = null)
        {
            var members = InfoResponseParse.TryAddMultipleMemberByExcelList(responseContent)
                ?? throw new InvalidOperationException(
                    "Could not parse AddMultipleMemberByExcel response. Expected a JSON array.");

            var columnOrder = columnNames is { Count: > 0 }
                ? columnNames
                : InfoResponseParse.GetAddMultipleMemberByExcelPropertyNames();

            var rows = members
                .Select(m =>
                {
                    var allProps = InfoResponseParse
                        .ToAddMultiMemberByExcelPropertyDictionary(m, includeEmpty: true)
                        .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

                    return columnOrder.ToDictionary(
                        col => col,
                        col => allProps.TryGetValue(col, out var v) ? v : string.Empty,
                        StringComparer.OrdinalIgnoreCase);
                })
                .ToList();

            ExcelReader.ReplaceSheetData(
                excelFileName,
                responseSheetName,
                rows,
                columnOrder);

            return members;
        }

        public static IList<AddMultipleMemberByExcel_ResponseModel>? SaveAddMultiMemberByExcelFromResponse(
            string? responseContent,
            string appSettingsFile = AppSettingsFile,
            bool updateEndpointId = true)
        {
            var members = InfoResponseParse.TryAddMultipleMemberByExcelList(responseContent)
                ?? throw new InvalidOperationException(
                    "Could not parse AddMultipleMemberByExcel response. Expected a JSON array.");

            var sectionValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["MemberCount"] = members.Count.ToString()
            };

            if (members.Count > 0)
            {
                var firstMemberProperties = InfoResponseParse.ToAddMultiMemberByExcelPropertyDictionary(members[0]);
                foreach (var (key, value) in firstMemberProperties)
                    sectionValues[key] = value;
            }

            ConfigReaderNew.UpdateJsonSection(appSettingsFile, AddMultipleMemberByExcelInfoSection, sectionValues);

            foreach (var (key, value) in sectionValues)
                ConfigReaderNew.UpdateJsonValue(appSettingsFile, key, value);

            if (updateEndpointId && members.Count > 0
                && !string.IsNullOrWhiteSpace(members[0].MemberId))
            {
                UpdateResponseValuesInJsonFile(appSettingsFile, "EndpointJson", "MemberId", members[0].MemberId);
            }

            return members;
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
