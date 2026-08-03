using EnterpriseApiAutomationFramework.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EnterpriseApiAutomationFramework.Core.Authentication
{
    public class InfoResponseParse
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public static GetSessionInfo? TryGetSessionInfo(string? responseContent)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<GetSessionInfo>(responseContent, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public static CheckExistingUser_ResponseModel? TryCheckExistingUserInfo(string? responseContent)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<CheckExistingUser_ResponseModel>(responseContent, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public static IList<AddMultipleMemberByExcel_ResponseModel>? TryAddMultipleMemberByExcelList(
            string? responseContent)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
                return null;

            try
            {
                var list = JsonSerializer.Deserialize<List<AddMultipleMemberByExcel_ResponseModel>>(
                    responseContent, JsonOptions);

                if (list is { Count: > 0 })
                    return list;
            }
            catch
            {
                // fall through — try single object
            }

            try
            {
                var single = JsonSerializer.Deserialize<AddMultipleMemberByExcel_ResponseModel>(
                    responseContent, JsonOptions);

                return single == null ? null : new List<AddMultipleMemberByExcel_ResponseModel> { single };
            }
            catch
            {
                return null;
            }
        }

        public static AddMultipleMemberByExcel_ResponseModel? TryAddMultipleMemberByExcelInfo(string? responseContent)
        {
            var list = TryAddMultipleMemberByExcelList(responseContent);
            return list is { Count: > 0 } ? list[0] : null;
        }

        public static string? TryGetSessionInfoValue(string? responseContent, string propertyName)
        {
            var sessionInfo = TryGetSessionInfo(responseContent);

            if (sessionInfo == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return null;
            }

            var property = typeof(GetSessionInfo).GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property == null)
            {
                return null;
            }

            return property.GetValue(sessionInfo)?.ToString();
        }

        public static string? TryExistingUserInfoValue(string? responseContent, string propertyName)
        {
            var existingUserInfo = TryCheckExistingUserInfo(responseContent);

            if (existingUserInfo == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return null;
            }

            var property = typeof(CheckExistingUser_ResponseModel).GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property == null)
            {
                return null;
            }

            return property.GetValue(existingUserInfo)?.ToString();
        }

        public static IReadOnlyDictionary<string, string> ToGetSessionPropertyDictionary(GetSessionInfo sessionInfo)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in typeof(GetSessionInfo).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = property.GetValue(sessionInfo)?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    result[property.Name] = value;
                }
            }

            return result;
        }

        public static IReadOnlyDictionary<string, string> ToCheckExistingUserPropertyDictionary(CheckExistingUser_ResponseModel existingUserInfo)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in typeof(CheckExistingUser_ResponseModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = property.GetValue(existingUserInfo)?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    result[property.Name] = value;
                }
            }

            return result;
        }

        public static IReadOnlyDictionary<string, string> ToAddMultiMemberByExcelPropertyDictionary(
            AddMultipleMemberByExcel_ResponseModel multipleMemberInfo,
            bool includeEmpty = false)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in typeof(AddMultipleMemberByExcel_ResponseModel)
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = property.GetValue(multipleMemberInfo)?.ToString() ?? string.Empty;
                if (includeEmpty || !string.IsNullOrWhiteSpace(value))
                    result[property.Name] = value;
            }

            return result;
        }

        public static IReadOnlyList<string> GetAddMultipleMemberByExcelPropertyNames() =>
            typeof(AddMultipleMemberByExcel_ResponseModel)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToList();

        public static GetPACFByBusinessUnitId_ResponseModel? TryGetPACFBusinessUnitInfo(string? responseContent)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
                return null;

            try
            {
                return JsonSerializer.Deserialize<GetPACFByBusinessUnitId_ResponseModel>(
                    responseContent, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Flattens PACF business-unit fields for appsettings section storage.
        /// Lists are serialized as JSON so nested data is preserved without colliding
        /// with SessionInfo flat keys (EmailId, BusinessUnitId, etc.).
        /// </summary>
        public static IReadOnlyDictionary<string, string> ToPACFBusinessUnitPropertyDictionary(
            GetPACFByBusinessUnitId_ResponseModel businessUnit)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in typeof(GetPACFByBusinessUnitId_ResponseModel)
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var raw = property.GetValue(businessUnit);
                if (raw is null)
                    continue;

                string value;
                if (raw is System.Collections.IEnumerable enumerable and not string)
                {
                    value = JsonSerializer.Serialize(raw, JsonOptions);
                }
                else
                {
                    value = raw.ToString() ?? string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(value))
                    result[property.Name] = value;
            }

            return result;
        }
    }
}
