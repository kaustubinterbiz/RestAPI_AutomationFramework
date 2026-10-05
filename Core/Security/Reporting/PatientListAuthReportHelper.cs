using System.Text.RegularExpressions;
using EnterpriseApiAutomationFramework.Core.Security.Authentication;
using EnterpriseApiAutomationFramework.Core.Security.Patient;

namespace EnterpriseApiAutomationFramework.Core.Security.Reporting;

/// <summary>
/// Maps Patient List flexible-step auth scenarios (PAT-01–08) to vulnerability types
/// and human-readable failure descriptions for the security living report.
/// </summary>
public static class PatientListAuthReportHelper
{
    private static readonly Regex PatScenarioRegex = new(@"\bPAT-(\d+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FlexibleTokenRegex = new(@"token ""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ResponseBodySuffixRegex = new(
        @",?\s*response body was:\s*[\s\S]*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsPatientListAuthScenario(SecurityScenarioCapture capture)
    {
        if (PatScenarioRegex.IsMatch(capture.ScenarioName))
            return true;

        if (capture.FeatureName.Contains("Patient List", StringComparison.OrdinalIgnoreCase))
            return true;

        var joined = string.Join(' ', capture.StepTexts);
        return joined.Contains("endpoint \"patientList\"", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPatScenarioName(string scenarioName) =>
        PatScenarioRegex.IsMatch(scenarioName);

    public static bool TryEnrichMetadata(SecurityScenarioCapture capture)
    {
        if (!IsPatientListAuthScenario(capture))
            return false;

        var patMatch = PatScenarioRegex.Match(capture.ScenarioName);
        if (patMatch.Success)
            capture.TestCaseId = patMatch.Value.ToUpperInvariant();

        capture.EndpointKey = "patientList";
        capture.HttpMethod = "POST";
        capture.VulnerabilityType = InferVulnerabilityType(capture.StepTexts);
        return true;
    }

    public static string InferVulnerabilityType(IReadOnlyList<string> stepTexts)
    {
        var joined = string.Join(' ', stepTexts);
        var tokenMode = ParseFlexibleTokenMode(stepTexts);

        if (!string.IsNullOrWhiteSpace(tokenMode))
        {
            return tokenMode.ToLowerInvariant() switch
            {
                "none" => ApiSecurityAuthConstants.ScenarioNoAuthHeader,
                "empty" => ApiSecurityAuthConstants.ScenarioEmptyBearer,
                "garbage" => ApiSecurityAuthConstants.ScenarioInvalidToken,
                "malformed" => ApiSecurityAuthConstants.ScenarioMalformedToken,
                "current" => InferCurrentTokenVulnerability(joined),
                _ => ApiSecurityAuthConstants.ScenarioInvalidToken
            };
        }

        return InferCurrentTokenVulnerability(joined);
    }

    public static bool TryDescribeFailure(
        SecurityScenarioCapture capture,
        int? expectedStatus,
        int? actualStatus,
        out string description)
    {
        description = string.Empty;

        if (!IsPatientListAuthScenario(capture))
            return false;

        if (!expectedStatus.HasValue || !actualStatus.HasValue)
            return false;

        var testCaseId = capture.TestCaseId ?? ExtractPatId(capture.ScenarioName) ?? "PAT-??";
        var vulnerability = capture.VulnerabilityType ?? InferVulnerabilityType(capture.StepTexts);
        var passed = expectedStatus == actualStatus;

        description = passed
            ? BuildPassDescription(testCaseId, vulnerability, actualStatus.Value)
            : BuildFailDescription(testCaseId, vulnerability, expectedStatus.Value, actualStatus.Value);

        return true;
    }

    public static string SanitizeAssertionError(string? rawError)
    {
        if (string.IsNullOrWhiteSpace(rawError))
            return string.Empty;

        var cleaned = ResponseBodySuffixRegex.Replace(rawError.Trim(), string.Empty).Trim();
        if (cleaned.StartsWith("Expected ", StringComparison.OrdinalIgnoreCase)
            && cleaned.Contains("because", StringComparison.OrdinalIgnoreCase))
        {
            var becauseIndex = cleaned.IndexOf(" because ", StringComparison.OrdinalIgnoreCase);
            if (becauseIndex > 0)
                cleaned = cleaned[..becauseIndex].TrimEnd('.');
        }

        return cleaned;
    }

    private static string? ParseFlexibleTokenMode(IReadOnlyList<string> stepTexts)
    {
        for (var i = stepTexts.Count - 1; i >= 0; i--)
        {
            if (!stepTexts[i].Contains("patientList", StringComparison.OrdinalIgnoreCase))
                continue;

            var match = FlexibleTokenRegex.Match(stepTexts[i]);
            if (match.Success)
                return match.Groups[1].Value.Trim();
        }

        var joinedMatch = FlexibleTokenRegex.Match(string.Join(' ', stepTexts));
        return joinedMatch.Success ? joinedMatch.Groups[1].Value.Trim() : null;
    }

    private static string InferCurrentTokenVulnerability(string joinedSteps)
    {
        if (joinedSteps.Contains("wrong issuer audience", StringComparison.OrdinalIgnoreCase))
            return PatientSecurityConstants.ScenarioWrongIssuerAudience;

        if (joinedSteps.Contains("missing claim", StringComparison.OrdinalIgnoreCase))
            return PatientSecurityConstants.ScenarioMissingClaim;

        if (joinedSteps.Contains("tampered access token", StringComparison.OrdinalIgnoreCase))
            return ApiSecurityAuthConstants.ScenarioTamperedToken;

        if (joinedSteps.Contains("expired access token", StringComparison.OrdinalIgnoreCase))
            return ApiSecurityAuthConstants.ScenarioExpiredToken;

        return ApiSecurityAuthConstants.ScenarioInvalidToken;
    }

    private static string? ExtractPatId(string scenarioName)
    {
        var match = PatScenarioRegex.Match(scenarioName);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private static string BuildPassDescription(string testCaseId, string vulnerability, int actualStatus) =>
        $"[{testCaseId}] Patient List API correctly returned HTTP {actualStatus} for {FormatVulnerabilityLabel(vulnerability)}. JWT authentication gate is enforced.";

    private static string BuildFailDescription(
        string testCaseId,
        string vulnerability,
        int expectedStatus,
        int actualStatus)
    {
        var condition = DescribeAuthCondition(vulnerability);
        var breachDetail = DescribeSecurityBreach(vulnerability, actualStatus);

        return $"[{testCaseId}] SECURITY BREACH — Patient List API returned HTTP {actualStatus} when {condition} (expected HTTP {expectedStatus}). " +
               $"{breachDetail} " +
               $"Root cause: JWT validation is missing or bypassed on POST /api/v2/Patient/{{businessunitId}}/Patients (OWASP API2: Broken Authentication).";
    }

    private static string DescribeSecurityBreach(string vulnerability, int actualStatus)
    {
        if (actualStatus is not (>= 200 and < 300))
        {
            return "The API did not reject the invalid auth attempt with 401 as required.";
        }

        return IsUnauthenticatedCase(vulnerability)
            ? "The API granted access without any valid credentials — unauthenticated callers can retrieve patient records (PHI exposure)."
            : "The API accepted an invalid/wrong token and still granted access — authentication is effectively bypassed and patient records (PHI) were returned to an unauthorized caller.";
    }

    private static bool IsUnauthenticatedCase(string vulnerability) =>
        vulnerability.ToUpperInvariant() is "NOAUTHHEADER" or "EMPTYBEARER" or "MISSINGBEARER" or "EMPTYTOKEN";

    private static string DescribeAuthCondition(string vulnerability) =>
        vulnerability.ToUpperInvariant() switch
        {
            "NOAUTHHEADER" => "no Authorization header was sent",
            "EMPTYBEARER" or "MISSINGBEARER" or "EMPTYTOKEN" => "an empty Bearer token was sent",
            "INVALIDTOKEN" => "a garbage/invalid JWT was sent (abc.def.ghi)",
            "MALFORMEDTOKEN" => "a malformed JWT was sent (not three Base64 segments)",
            "EXPIREDTOKEN" => "an expired access token was used",
            "TAMPEREDTOKEN" or "TAMPEREDSIGNATURE" => "a tampered JWT signature was used",
            "WRONGISSUERAUDIENCE" => "a JWT with wrong issuer or audience was used",
            "MISSINGCLAIM" => "a JWT missing a required claim was used",
            _ => $"auth case '{vulnerability}' was applied"
        };

    private static string FormatVulnerabilityLabel(string vulnerability) =>
        vulnerability switch
        {
            ApiSecurityAuthConstants.ScenarioNoAuthHeader => "missing Authorization header",
            ApiSecurityAuthConstants.ScenarioEmptyBearer => "empty Bearer token",
            ApiSecurityAuthConstants.ScenarioInvalidToken => "invalid garbage JWT",
            ApiSecurityAuthConstants.ScenarioMalformedToken => "malformed JWT",
            ApiSecurityAuthConstants.ScenarioExpiredToken => "expired access token",
            ApiSecurityAuthConstants.ScenarioTamperedToken => "tampered JWT",
            PatientSecurityConstants.ScenarioWrongIssuerAudience => "wrong issuer/audience JWT",
            PatientSecurityConstants.ScenarioMissingClaim => "JWT with missing required claim",
            _ => vulnerability
        };
}
