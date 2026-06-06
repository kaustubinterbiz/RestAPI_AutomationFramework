using EnterpriseApiAutomationFramework.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EnterpriseApiAutomationFramework.Core.Authentication
{
    public class InfoResponseParse
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
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
    }
}
