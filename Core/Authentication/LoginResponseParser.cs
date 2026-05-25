using System.Text.Json;
using EnterpriseApiAutomationFramework.Models.Response;

namespace EnterpriseApiAutomationFramework.Core.Authentication;

public static class LoginResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string? TryGetAccessToken(string? responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return null;
        }

        try
        {
            var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, JsonOptions);
            return string.IsNullOrWhiteSpace(loginResponse?.access_token)
                ? null
                : loginResponse.access_token;
        }
        catch
        {
            return null;
        }
    }
}
