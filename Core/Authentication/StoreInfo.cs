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
        public const string BusinessUnitInfoSection = "BusinessUnitInfo";

        //Session Info — full body + extracted keys go to RequestEndPoint.xlsx Endpoint_Response
        public static GetSessionInfo SaveSessionInfoFromResponse(
            string? responseContent,
            string appSettingsFile = AppSettingsFile,
            bool updateEndpointId = true)
        {
            var sessionInfo = InfoResponseParse.TryGetSessionInfo(responseContent)
                ?? throw new InvalidOperationException(
                    "Could not parse GetSessionInfo from the last API response.");

            var properties = InfoResponseParse.ToGetSessionPropertyDictionary(sessionInfo);

            SaveApiResponse(
                TestConfigDefaults.SessionInfoApiKey,
                responseContent,
                primaryValue: sessionInfo.CacheId);

            if (updateEndpointId)
                UpsertExtractedKeys(properties);

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

            SaveApiResponse(
                TestConfigDefaults.ExistingUserApiKey,
                responseContent,
                primaryValue: existingUserInfo.MemberId);

            if (updateEndpointId)
                UpsertExtractedKeys(properties);

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

            var primaryMemberId = members.Count > 0 ? members[0].MemberId : null;

            SaveApiResponse(
                TestConfigDefaults.AddMultipleMemberByExcelApiKey,
                responseContent,
                primaryValue: primaryMemberId);

            if (updateEndpointId && !string.IsNullOrWhiteSpace(primaryMemberId))
                UpdateEndpointStoredValue("MemberId", primaryMemberId);

            if (members.Count > 0)
            {
                var firstMemberProperties = InfoResponseParse.ToAddMultiMemberByExcelPropertyDictionary(members[0]);
                UpsertExtractedKeys(firstMemberProperties);
                UpdateEndpointStoredValue("MemberCount", members.Count.ToString());
            }

            return members;
        }

        /// <summary>
        /// Persists GetPACFByBusinessUnitID into appsettings BusinessUnitInfo section only.
        /// Does not write flat top-level keys (EmailId, BusinessUnitId, …) so Login/session stay intact.
        /// </summary>
        public static GetPACFByBusinessUnitId_ResponseModel SavePACFBusinessUnitFromResponse(
            string? responseContent,
            string appSettingsFile = AppSettingsFile)
        {
            var businessUnit = InfoResponseParse.TryGetPACFBusinessUnitInfo(responseContent)
                ?? throw new InvalidOperationException(
                    "Could not parse GetPACFByBusinessUnitID from the last API response.");

            var properties = InfoResponseParse.ToPACFBusinessUnitPropertyDictionary(businessUnit);
            ConfigReaderNew.UpdateJsonSection(appSettingsFile, BusinessUnitInfoSection, properties);

            return businessUnit;
        }

        /// <summary>
        /// Central store: API key name + full response body in RequestEndPoint.xlsx → Endpoint_Response.
        /// </summary>
        public static void SaveApiResponse(
            string apiKey,
            string? responseBody,
            string? httpStatus = null,
            string? primaryValue = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

            try
            {
                ExcelConfigWriter.UpsertApiResponse(
                    apiKey,
                    responseBody ?? string.Empty,
                    httpStatus,
                    primaryValue);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or TimeoutException)
            {
                // Full response body mirror; do not fail when Excel/WPS locks the workbook.
                System.Diagnostics.Debug.WriteLine(
                    $"Excel Endpoint_Response upsert skipped for API key '{apiKey}': {ex.Message}");
            }
        }

        private static void UpsertExtractedKeys(IReadOnlyDictionary<string, string> properties)
        {
            foreach (var (key, value) in properties)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    UpdateEndpointStoredValue(key, value);
            }
        }

        private static void UpdateEndpointStoredValue(string updateOnKey, string storedValue)
        {
            try
            {
                ExcelConfigWriter.UpsertEndpointResponse(updateOnKey, storedValue);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or TimeoutException)
            {
                // Extracted CacheId/MemberId mirrors; do not fail when VS debug locks the workbook.
                System.Diagnostics.Debug.WriteLine(
                    $"Excel Endpoint_Response upsert skipped for '{updateOnKey}': {ex.Message}");
            }
        }

        //GetSessionInfo and CacheId — prefer Excel Endpoint_Response (source of truth after store)
        public static string? GetCachedValue(string key, string appSettingsFile = AppSettingsFile)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var fromExcel = ExcelConfigReader.GetEndpointResponseValue(key);
            if (!string.IsNullOrWhiteSpace(fromExcel))
                return fromExcel;

            ConfigReaderNew.LoadConfig(appSettingsFile);

            var sectionValue = ConfigReaderNew.GetJsonSectionValue(
                appSettingsFile,
                SessionInfoSection,
                key);

            if (!string.IsNullOrWhiteSpace(sectionValue))
                return sectionValue;

            var flatValue = ConfigReaderNew.GetJsonValue(appSettingsFile, key);
            return string.IsNullOrWhiteSpace(flatValue) ? null : flatValue;
        }

        //GetExistingUserInfo and MemberId
        public static string? GetMemberIdValue(string key, string appSettingsFile = AppSettingsFile)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var fromExcel = ExcelConfigReader.GetEndpointResponseValue(key);
            if (!string.IsNullOrWhiteSpace(fromExcel))
                return fromExcel;

            ConfigReaderNew.LoadConfig(appSettingsFile);

            var existingUserValue = ConfigReaderNew.GetJsonSectionValue(
                appSettingsFile,
                CheckExistingUserInfoSection,
                key);

            if (!string.IsNullOrWhiteSpace(existingUserValue))
                return existingUserValue;

            var flatValue = ConfigReaderNew.GetJsonValue(appSettingsFile, key);
            return string.IsNullOrWhiteSpace(flatValue) ? null : flatValue;
        }
    }
}
