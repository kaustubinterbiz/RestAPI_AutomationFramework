using System.Reflection;
using System.Text.Json;
using EnterpriseApiAutomationFramework.Models.Response;

namespace EnterpriseApiAutomationFramework.Core.Authentication;

public static class GetSessionInfoResponseParse
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

    public static IReadOnlyDictionary<string, string> ToPropertyDictionary(GetSessionInfo sessionInfo)
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
}
