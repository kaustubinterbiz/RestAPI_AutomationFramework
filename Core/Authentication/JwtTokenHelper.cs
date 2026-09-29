using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EnterpriseApiAutomationFramework.Core.Authentication;

internal static class JwtTokenHelper
{
    private const long PastExpUnixSeconds = 1_577_836_800; // 2020-01-01 UTC

    public static DateTimeOffset? GetExpiryUtc(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return null;
        }

        if (!TryGetParts(jwt, out var parts))
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

    /// <summary>
    /// Produces a structurally valid JWT whose <c>exp</c> claim is in the past.
    /// Keeps the original header and signature so this is not the same as InvalidToken.
    /// </summary>
    public static string WithExpiredExpClaim(string jwt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jwt);

        if (!TryGetParts(jwt, out var parts) || parts.Length < 3)
        {
            throw new ArgumentException(
                "WithExpiredExpClaim requires a three-part JWT (header.payload.signature).",
                nameof(jwt));
        }

        var payload = ParsePayloadObject(parts[1]);
        payload["exp"] = PastExpUnixSeconds;
        if (payload["nbf"] != null)
            payload["nbf"] = PastExpUnixSeconds - 60;

        var newPayload = EncodeBase64Url(Encoding.UTF8.GetBytes(payload.ToJsonString()));
        // Keep original signature → server must reject if it validates signature/exp.
        return $"{parts[0]}.{newPayload}.{parts[2]}";
    }

    /// <summary>
    /// Starts from a genuine JWT, modifies a payload claim, keeps the original signature.
    /// Prefer <paramref name="preferredClaim"/> (default <c>sub</c>); otherwise first mutable claim.
    /// </summary>
    /// <summary>Valid JWT with last signature character flipped (signature validation should fail).</summary>
    public static string WithTamperedSignature(string jwt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jwt);
        if (!TryGetParts(jwt, out var parts) || parts.Length < 3)
            throw new ArgumentException("WithTamperedSignature requires a three-part JWT.", nameof(jwt));

        var signature = parts[2];
        var last = signature[^1] == 'a' ? 'b' : 'a';
        return $"{parts[0]}.{parts[1]}.{signature[..^1]}{last}";
    }

    /// <summary>JWT with iss/aud replaced to simulate cross-tenant token.</summary>
    public static string WithWrongIssuerAudience(string jwt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jwt);
        if (!TryGetParts(jwt, out var parts) || parts.Length < 3)
            throw new ArgumentException("WithWrongIssuerAudience requires a three-part JWT.", nameof(jwt));

        var payload = ParsePayloadObject(parts[1]);
        payload["iss"] = "https://wrong-tenant.example.com/";
        payload["aud"] = "00000000-0000-0000-0000-000000000099";
        var newPayload = EncodeBase64Url(Encoding.UTF8.GetBytes(payload.ToJsonString()));
        return $"{parts[0]}.{newPayload}.{parts[2]}";
    }

    /// <summary>JWT with businessunit/member-like claims removed for missing-claim tests.</summary>
    public static string WithoutBusinessClaims(string jwt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jwt);
        if (!TryGetParts(jwt, out var parts) || parts.Length < 3)
            throw new ArgumentException("WithoutBusinessClaims requires a three-part JWT.", nameof(jwt));

        var payload = ParsePayloadObject(parts[1]);
        foreach (var key in payload.ToList().Select(p => p.Key))
        {
            if (key.Contains("business", StringComparison.OrdinalIgnoreCase)
                || key.Contains("member", StringComparison.OrdinalIgnoreCase)
                || key.Contains("unit", StringComparison.OrdinalIgnoreCase))
            {
                payload.Remove(key);
            }
        }

        var newPayload = EncodeBase64Url(Encoding.UTF8.GetBytes(payload.ToJsonString()));
        return $"{parts[0]}.{newPayload}.{parts[2]}";
    }

    public static string TamperPayloadClaim(string jwt, string preferredClaim = "sub")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jwt);

        if (!TryGetParts(jwt, out var parts) || parts.Length < 3)
        {
            throw new ArgumentException(
                "TamperPayloadClaim requires a three-part JWT (header.payload.signature).",
                nameof(jwt));
        }

        var payload = ParsePayloadObject(parts[1]);
        if (!TryTamperClaim(payload, preferredClaim) && !TryTamperAnyStringClaim(payload))
        {
            // Last resort: inject a claim that was not present.
            payload["tampered_by_security_test"] = "999";
        }

        var newPayload = EncodeBase64Url(Encoding.UTF8.GetBytes(payload.ToJsonString()));
        return $"{parts[0]}.{newPayload}.{parts[2]}";
    }

    private static bool TryTamperClaim(JsonObject payload, string claimName)
    {
        if (!payload.TryGetPropertyValue(claimName, out var node) || node is null)
            return false;

        if (node is JsonValue value)
        {
            if (value.TryGetValue<string>(out var text))
            {
                payload[claimName] = string.IsNullOrEmpty(text) ? "tampered-security-test" : text + "-TAMPERED";
                return true;
            }

            if (value.TryGetValue<long>(out var number))
            {
                payload[claimName] = number + 1;
                return true;
            }
        }

        if (node is JsonArray { Count: > 0 } array && array[0] is JsonValue first
            && first.TryGetValue<string>(out var email))
        {
            array[0] = string.IsNullOrEmpty(email) ? "tampered@security.test" : "tampered-" + email;
            return true;
        }

        return false;
    }

    private static bool TryTamperAnyStringClaim(JsonObject payload)
    {
        foreach (var key in new[] { "sub", "oid", "name", "preferred_username", "emails", "nonce" })
        {
            if (TryTamperClaim(payload, key))
                return true;
        }

        foreach (var property in payload.ToList())
        {
            if (property.Key is "exp" or "nbf" or "iat" or "ver")
                continue;

            if (TryTamperClaim(payload, property.Key))
                return true;
        }

        return false;
    }

    private static JsonObject ParsePayloadObject(string payloadSegment)
    {
        var bytes = DecodeBase64Url(payloadSegment);
        var node = JsonNode.Parse(Encoding.UTF8.GetString(bytes)) as JsonObject
            ?? throw new InvalidOperationException("JWT payload is not a JSON object.");
        return node;
    }

    private static bool TryGetParts(string jwt, out string[] parts)
    {
        parts = jwt.Split('.');
        return parts.Length >= 2;
    }

    private static string EncodeBase64Url(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
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
