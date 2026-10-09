using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using EnterpriseApiAutomationFramework.Core.Security.Patient;

namespace EnterpriseApiAutomationFramework.Core.Security.PatientSearch;

public static class PatientSearchResponseAssert
{
    private static readonly Regex InternalLeakPattern = new(
        @"(System\.| at \w+\.|SqlException|stack trace|access_token\s*"")",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string? ValidateHttpStatus(int actual, int expected) =>
        actual == expected ? null : $"Expected HTTP {expected}, got {actual}.";

    public static string? ValidateAuthRejection(int actual)
    {
        if (actual is >= 200 and < 300)
            return "Authentication failure expected (401/403), but API returned success.";

        if (!PatientSearchSecurityConstants.AuthRejectionStatuses.Contains(actual))
            return $"Expected auth rejection (401|403), got {actual}.";

        return null;
    }

    public static string? ValidateNoInternalLeak(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        return InternalLeakPattern.IsMatch(body)
            ? "Response may expose internal details."
            : null;
    }

    public static string? ValidateSearchOutcome(string? body, int status, string outcome)
    {
        if (status != 200)
        {
            return status switch
            {
                401 => "Search should return HTTP 200, but got 401 Unauthorized. Check Bearer token, CacheId, and Apim access.",
                403 => "Search should return HTTP 200, but got 403 Forbidden.",
                _ => $"Search should return HTTP 200, but got {status}."
            };
        }

        var parseError = TryParseEnvelope(body, out var root);
        if (parseError != null)
            return parseError;

        var data = root!["Data"]?.AsArray();
        if (data == null)
            return "Response JSON must contain Data array.";

        return outcome.Trim().ToLowerInvariant() switch
        {
            "returns patients" => ValidateHasMatches(data),
            "returns no patients" => ValidateEmptyData(data, root!),
            "includes expected patient" => ValidateContainsPatientId(data, null),
            _ => $"Unknown outcome '{outcome}'. Use: returns patients | returns no patients | includes expected patient."
        };
    }

    public static string? ValidateSearchSucceedsForUser(string? body, int status) =>
        ValidateSearchShouldSucceed(body, status);

    public static string? ValidateOtherOrgPatientNotInResponse(string? body, string orgBOtherPatientId)
    {
        var leak = ValidateNoInternalLeak(body);
        if (leak != null)
            return leak;

        return ValidateMustNotExposeOtherOrgPatient(body, orgBOtherPatientId);
    }

    private static string? ValidateSearchShouldSucceed(string? body, int status)
    {
        if (status is not (>= 200 and < 300))
        {
            return status switch
            {
                401 => "Search should succeed (HTTP 200), but got 401 Unauthorized. Check Bearer token, CacheId header, and Apim access.",
                403 => "Search should succeed (HTTP 200), but got 403 Forbidden. User may not be allowed to search.",
                _ => $"Search should succeed (HTTP 2xx), but got {status}."
            };
        }

        return TryParseEnvelope(body, out var env) ?? (env!["Data"] is JsonArray
            ? null
            : "Success response must contain a Data array.");
    }

    private static string? ValidateMustNotExposeOtherOrgPatient(string? body, string orgBOtherPatientId)
    {
        if (string.IsNullOrWhiteSpace(orgBOtherPatientId))
        {
            return "Configure PatientSecurityTestData:OrgBPatientId for cross-org leak check.";
        }

        if (body != null && body.Contains(orgBOtherPatientId, StringComparison.OrdinalIgnoreCase))
        {
            return $"Other organization's patient id must not appear in search results (found '{orgBOtherPatientId}').";
        }

        return null;
    }

    private static string? ValidateHasMatches(JsonArray data)
    {
        if (data.Count == 0)
            return "Expected at least one patient in Data, but array is empty.";

        foreach (var item in data)
        {
            var patientId = item?["PatientId"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(patientId))
                return "Each patient record must have a non-empty PatientId.";
        }

        return null;
    }

    private static string? ValidateEmptyData(JsonArray data, JsonObject root)
    {
        if (data.Count != 0)
            return $"Expected empty Data array, but got {data.Count} record(s).";

        _ = root["Status"];
        _ = root["TotalRecords"];
        return null;
    }

    private static string? ValidateContainsPatientId(JsonArray data, string? expectedPatientId)
    {
        if (string.IsNullOrWhiteSpace(expectedPatientId))
            expectedPatientId = PatientSearchSecurityTestDataConfig.ExpectedPatientId;

        if (string.IsNullOrWhiteSpace(expectedPatientId))
            return ValidateHasMatches(data);

        foreach (var item in data)
        {
            var id = item?["PatientId"]?.GetValue<string>();
            if (string.Equals(id, expectedPatientId, StringComparison.OrdinalIgnoreCase))
                return null;
        }

        return $"Expected patient id '{expectedPatientId}' was not found in Data.";
    }

    private static string? TryParseEnvelope(string? body, out JsonObject? root)
    {
        root = null;
        if (string.IsNullOrWhiteSpace(body))
            return "Response body is empty.";

        try
        {
            root = JsonNode.Parse(body)?.AsObject();
        }
        catch (JsonException)
        {
            return "Response body is not valid JSON.";
        }

        return root == null ? "Response root must be a JSON object." : null;
    }
}
