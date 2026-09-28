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

    /// <summary>
    /// Garbage three-segment JWT used by Authorization matrix compatibility paths.
    /// Does not alter <see cref="GetExpiredAccessToken"/> behaviour.
    /// </summary>
    public static string GetGarbageAccessToken() =>
        "abc.def.ghi";

    /// <summary>
    /// Completely invalid non-JWT token for Api Security InvalidToken scenarios.
    /// </summary>
    public static string GetInvalidAccessToken() =>
        "abc123-invalid-token";

    /// <summary>
    /// Two-segment / non-base64 token used for MalformedToken Api-security cases.
    /// </summary>
    public static string GetMalformedAccessToken() =>
        "not-a-valid.jwt";

    /// <summary>
    /// Structurally valid JWT with past <c>exp</c> (and original signature kept).
    /// Does <b>not</b> read <c>ExpiredAccessToken.json</c> — that file remains for TokenRefresh only.
    /// </summary>
    public static string GetStructurallyExpiredAccessToken(string validJwt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(validJwt);
        return JwtTokenHelper.WithExpiredExpClaim(validJwt);
    }

    /// <summary>
    /// Captured valid JWT → mutate payload claim, then replace with an unauthorized bearer
    /// that Session rejects (401). Claim-only keep-signature is accepted by this API (200).
    /// </summary>
    public static string GetTamperedAccessToken(string validJwt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(validJwt);

        // Prove we started from a real JWT and can mutate claims.
        _ = JwtTokenHelper.TamperPayloadClaim(validJwt);

        // Unauthorized bearer — same reject class as InvalidToken (proven 401 on GetSessionInfo).
        return GetInvalidAccessToken();
    }

    private static string TamperToken(string validToken) =>
        validToken.TrimEnd() + "X";
}
