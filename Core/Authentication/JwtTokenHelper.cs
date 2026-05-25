using System.Text;
using System.Text.Json;

namespace EnterpriseApiAutomationFramework.Core.Authentication;

internal static class JwtTokenHelper
{
    public static DateTimeOffset? GetExpiryUtc(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return null;
        }

        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payloadBytes = DecodeBase64Url(parts[1]);
            using var doc = JsonDocument.Parse(payloadBytes);
            if (doc.RootElement.TryGetProperty("exp", out var expElement)
                && expElement.TryGetInt64(out var unixSeconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    public static DateTimeOffset ResolveExpiry(string accessToken, int? expiresInSeconds)
    {
        var jwtExpiry = GetExpiryUtc(accessToken);
        if (jwtExpiry.HasValue)
        {
            return jwtExpiry.Value;
        }

        if (expiresInSeconds is > 0)
        {
            return DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds.Value);
        }

        return DateTimeOffset.UtcNow.AddHours(1);
    }

    private static byte[] DecodeBase64Url(string segment)
    {
        var padded = segment.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }
}
