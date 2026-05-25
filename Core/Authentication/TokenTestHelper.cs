using System.Text.Json;
using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Authentication;

/// <summary>
/// Supplies expired/invalid tokens for negative authentication scenarios.
/// </summary>
public static class TokenTestHelper
{
    private const string ExpiredTokenFile = "TestData/Login/ExpiredAccessToken.json";

    private const string FallbackInvalidJwt =
        "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2MDAwMDAwMDAsIm5iZiI6MTYwMDAwMDAwMCwic3ViIjoidGVzdC1leHBpcmVkIn0.invalid-signature";

    public static string GetExpiredAccessToken(string? currentValidToken = null)
    {
        var fromFile = LoadFromFile();
        if (!string.IsNullOrWhiteSpace(fromFile))
        {
            return fromFile;
        }

        if (!string.IsNullOrWhiteSpace(currentValidToken))
        {
            var expiry = JwtTokenHelper.GetExpiryUtc(currentValidToken);
            if (expiry.HasValue && expiry.Value < DateTimeOffset.UtcNow)
            {
                return currentValidToken;
            }

            return TamperToken(currentValidToken);
        }

        return FallbackInvalidJwt;
    }

    private static string? LoadFromFile()
    {
        try
        {
            var path = ConfigReaderNew.ResolvePathForRead(ExpiredTokenFile);
            if (!File.Exists(path))
            {
                return null;
            }

            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
            {
                var token = tokenElement.GetString();
                return string.IsNullOrWhiteSpace(token) ? null : token;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string TamperToken(string validToken) =>
        validToken.TrimEnd() + "X";
}
