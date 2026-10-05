using System.Text.RegularExpressions;
using EnterpriseApiAutomationFramework.Core.Authorization;
using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Core.Security.Authentication;
using EnterpriseApiAutomationFramework.Core.Security.Patient;

namespace EnterpriseApiAutomationFramework.Core.Security.Reporting;

/// <summary>
/// Enriches raw scenario captures with endpoint URLs, OWASP metadata, and narratives.
/// </summary>
public static class SecurityScenarioEnricher
{
    private static readonly Regex ApiPathTagRegex = new(@"^\[?(api/[^\]]+)\]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AuthTestIdRegex = new(@"(AUTH|IDOR|IV)-\d+-\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IReadOnlyList<SecurityExecutionResult> BuildExecutions(
        SecurityScenarioCapture capture,
        string suite,
        string apiBaseUrl)
    {
        if (capture.TrackerResults.Count > 0)
        {
            return capture.TrackerResults
                .Select(r => MapTrackerResult(r, capture, suite, apiBaseUrl))
                .ToList();
        }

        if (capture.LastActualStatus.HasValue)
        {
            var expected = capture.LastExpectedStatus
                ?? (!capture.OverallFailed ? capture.LastActualStatus : null);

            var errorMessage = ResolveExecutionErrorMessage(capture, expected, capture.LastActualStatus);

            return
            [
                MapSingleExecution(
                    capture,
                    suite,
                    apiBaseUrl,
                    capture.VulnerabilityType ?? ResolveDefaultVulnerability(suite),
                    capture.EndpointKey ?? "unknown",
                    capture.HttpMethod ?? "GET",
                    expected,
                    capture.LastActualStatus,
                    errorMessage)
            ];
        }

        return
        [
            MapSingleExecution(
                capture,
                suite,
                apiBaseUrl,
                capture.VulnerabilityType ?? ResolveDefaultVulnerability(suite),
                capture.EndpointKey ?? ExtractEndpointFromTags(capture.Tags) ?? "unknown",
                capture.HttpMethod ?? "GET",
                null,
                null,
                capture.ScenarioError ?? "No execution data recorded.",
                passedOverride: !capture.OverallFailed)
        ];
    }

    public static int? InferExpectedStatusFromSteps(IReadOnlyList<string> stepTexts)
    {
        foreach (var step in stepTexts)
        {
            var match = Regex.Match(
                step,
                @"(?:the API status code should be|Status code should be|Authorization status code should be)\s+(\d+)",
                RegexOptions.IgnoreCase);

            if (match.Success && int.TryParse(match.Groups[1].Value, out var code))
                return code;
        }

        return null;
    }

    public static void InferMetadataFromSteps(SecurityScenarioCapture capture)
    {
        foreach (var step in capture.StepTexts)
        {
            var authMatch = AuthTestIdRegex.Match(step);
            if (authMatch.Success)
            {
                capture.TestCaseId = authMatch.Value;
                break;
            }
        }

        if (!string.IsNullOrWhiteSpace(capture.TestCaseId))
        {
            var entry = ApiSecurityAuthMatrixReader.GetAuthenticationRows(enabledOnly: false)
                .FirstOrDefault(r => string.Equals(r.TestCaseId, capture.TestCaseId, StringComparison.OrdinalIgnoreCase));

            if (entry != null)
            {
                capture.VulnerabilityType = entry.ScenarioType;
                capture.EndpointKey = entry.EndpointKey;
                capture.HttpMethod = entry.HttpMethod;
                return;
            }

            var patientEntry = PatientSecurityMatrixReader.FindByTestCaseId(capture.TestCaseId);
            if (patientEntry != null)
            {
                capture.VulnerabilityType = patientEntry.ScenarioType;
                capture.EndpointKey = patientEntry.EndpointKey;
                capture.HttpMethod = patientEntry.HttpMethod;
                return;
            }
        }

        var endpointMatch = Regex.Match(
            string.Join(' ', capture.StepTexts),
            @"endpoint ""([^""]+)"" method ""([^""]+)"" role ""([^""]+)""",
            RegexOptions.IgnoreCase);

        if (endpointMatch.Success)
        {
            capture.EndpointKey = endpointMatch.Groups[1].Value;
            capture.HttpMethod = endpointMatch.Groups[2].Value;
            capture.Role = endpointMatch.Groups[3].Value;
            capture.VulnerabilityType = "EndpointAccess";
            return;
        }

        var tokenMatch = Regex.Match(
            string.Join(' ', capture.StepTexts),
            @"token scenario ""([^""]+)"" for role ""([^""]+)""",
            RegexOptions.IgnoreCase);

        if (tokenMatch.Success)
        {
            capture.VulnerabilityType = tokenMatch.Groups[1].Value;
            capture.Role = tokenMatch.Groups[2].Value;
            capture.HttpMethod = "GET";
            capture.EndpointKey = "get";
            return;
        }

        if (PatientListAuthReportHelper.TryEnrichMetadata(capture))
            return;

        var allSteps = string.Join(' ', capture.StepTexts);
        if (allSteps.Contains("tampered access token", StringComparison.OrdinalIgnoreCase))
        {
            capture.VulnerabilityType = ApiSecurityAuthConstants.ScenarioTamperedToken;
            capture.HttpMethod = "GET";
            capture.EndpointKey = ExtractEndpointFromTags(capture.Tags) ?? "get";
            return;
        }

        InferFunctionalMetadata(capture);
    }

    private static void InferFunctionalMetadata(SecurityScenarioCapture capture)
    {
        if (!string.IsNullOrWhiteSpace(capture.VulnerabilityType))
            return;

        var tagEndpoint = ExtractEndpointFromTags(capture.Tags);
        if (!string.IsNullOrWhiteSpace(tagEndpoint))
            capture.EndpointKey = tagEndpoint;

        capture.HttpMethod ??= InferHttpMethodFromSteps(capture.StepTexts);

        capture.VulnerabilityType = InferFunctionalVulnerabilityType(capture);

        if (string.IsNullOrWhiteSpace(capture.EndpointKey))
            capture.EndpointKey = InferEndpointKeyFromFeature(capture.FeatureName, capture.StepTexts);
    }

    private static string InferFunctionalVulnerabilityType(SecurityScenarioCapture capture)
    {
        var tags = capture.Tags;
        var feature = capture.FeatureName;
        var steps = string.Join(' ', capture.StepTexts);

        if (tags.Any(t => t.Equals("token-expired", StringComparison.OrdinalIgnoreCase)))
            return "TokenExpired";

        if (tags.Any(t => t.Equals("token-refresh", StringComparison.OrdinalIgnoreCase)))
            return "TokenRefresh";

        if (tags.Any(t => t.Contains("Login", StringComparison.OrdinalIgnoreCase)))
            return "Login";

        if (tags.Any(t => t.Contains("AddMember", StringComparison.OrdinalIgnoreCase)))
            return "AddMember";

        if (tags.Any(t => t.Contains("BusinessUnit", StringComparison.OrdinalIgnoreCase)))
            return "BusinessUnit";

        if (feature.Contains("Token Refresh", StringComparison.OrdinalIgnoreCase))
            return "TokenRefresh";

        if (feature.Contains("Login", StringComparison.OrdinalIgnoreCase)
            || feature.Contains("Access Token", StringComparison.OrdinalIgnoreCase))
            return "Login";

        if (feature.Contains("Add Member", StringComparison.OrdinalIgnoreCase))
            return "AddMember";

        if (feature.Contains("Business Unit", StringComparison.OrdinalIgnoreCase))
            return "BusinessUnit";

        if (steps.Contains("expired access token", StringComparison.OrdinalIgnoreCase))
            return "TokenExpired";

        if (steps.Contains("LoginAsync", StringComparison.OrdinalIgnoreCase)
            || steps.Contains("valid login", StringComparison.OrdinalIgnoreCase)
            || steps.Contains("generates token", StringComparison.OrdinalIgnoreCase))
            return "Login";

        return "FunctionalApi";
    }

    private static string InferHttpMethodFromSteps(IReadOnlyList<string> stepTexts)
    {
        var joined = string.Join(' ', stepTexts);

        if (Regex.IsMatch(joined, @"\bPOST\b|sends POST|Send POST", RegexOptions.IgnoreCase))
            return "POST";

        if (Regex.IsMatch(joined, @"\bPUT\b|sends PUT", RegexOptions.IgnoreCase))
            return "PUT";

        if (Regex.IsMatch(joined, @"\bDELETE\b|sends DELETE", RegexOptions.IgnoreCase))
            return "DELETE";

        if (Regex.IsMatch(joined, @"\bPATCH\b|sends PATCH", RegexOptions.IgnoreCase))
            return "PATCH";

        return "GET";
    }

    private static string InferEndpointKeyFromFeature(string featureName, IReadOnlyList<string> stepTexts)
    {
        var joined = string.Join(' ', stepTexts);

        var endpointMatch = Regex.Match(
            joined,
            @"endpoint ""([^""]+)""|request ""([^""]+)""|key ""([^""]+)""",
            RegexOptions.IgnoreCase);

        if (endpointMatch.Success)
        {
            foreach (Group g in endpointMatch.Groups)
            {
                if (g.Index > 0 && !string.IsNullOrWhiteSpace(g.Value))
                    return g.Value;
            }
        }

        if (featureName.Contains("Business Unit", StringComparison.OrdinalIgnoreCase))
            return "getPACFByBusinessUnitID";

        return "unknown";
    }

    private static string ResolveDefaultVulnerability(string suite) =>
        suite.Equals("Functional", StringComparison.OrdinalIgnoreCase)
            ? "FunctionalApi"
            : "EndpointAccess";

    public static IReadOnlyList<SecurityStepExecution> BuildStepNarratives(
        SecurityScenarioCapture capture,
        IReadOnlyList<SecurityExecutionResult> executions)
    {
        var overallFailed = capture.OverallFailed;
        var failureReason = ResolveFailureReason(capture, executions);
        var failedStepText = ResolveFailedStepText(capture, overallFailed);
        var steps = new List<SecurityStepExecution>();

        for (var i = 0; i < capture.StepTexts.Count; i++)
        {
            var text = capture.StepTexts[i];
            var stepType = InferStepType(text);
            var narrative = BuildNarrative(text, capture, executions);
            var isFailedStep = overallFailed
                && !string.IsNullOrWhiteSpace(failedStepText)
                && string.Equals(text, failedStepText, StringComparison.OrdinalIgnoreCase);
            var status = isFailedStep ? SecurityTestStatus.Fail : SecurityTestStatus.Pass;

            if (!overallFailed)
                status = SecurityTestStatus.Pass;

            steps.Add(new SecurityStepExecution
            {
                StepText = text,
                StepType = stepType,
                Narrative = narrative,
                Status = status,
                FailureReason = isFailedStep ? failureReason : null
            });
        }

        return steps;
    }

    public static (string? FailedStepText, string? FailureSummary) ResolveFailureDetails(
        SecurityScenarioCapture capture,
        IReadOnlyList<SecurityExecutionResult> executions,
        IReadOnlyList<SecurityStepExecution> steps)
    {
        if (!capture.OverallFailed && !executions.Any(e => e.Status == SecurityTestStatus.Fail))
            return (null, null);

        var failedStep = ResolveFailedStepText(capture, true)
            ?? steps.LastOrDefault(s => s.Status == SecurityTestStatus.Fail)?.StepText;
        var reason = ResolveFailureReason(capture, executions);
        var summary = BuildFailureSummary(capture, executions, reason);
        return (failedStep, summary);
    }

    private static string? ResolveFailedStepText(SecurityScenarioCapture capture, bool overallFailed)
    {
        if (!overallFailed)
            return null;

        var aggregator = capture.StepTexts.FirstOrDefault(s =>
            s.Contains("all authorization executions should pass", StringComparison.OrdinalIgnoreCase));
        if (aggregator != null)
            return aggregator;

        for (var i = capture.StepTexts.Count - 1; i >= 0; i--)
        {
            var stepType = InferStepType(capture.StepTexts[i]);
            if (stepType is "Then" or "And")
                return capture.StepTexts[i];
        }

        return capture.StepTexts.Count > 0 ? capture.StepTexts[^1] : null;
    }

    private static string? ResolveFailureReason(
        SecurityScenarioCapture capture,
        IReadOnlyList<SecurityExecutionResult> executions)
    {
        var trackerFailure = capture.TrackerResults.FirstOrDefault(r => !r.Passed);
        if (trackerFailure != null && !string.IsNullOrWhiteSpace(trackerFailure.ErrorMessage))
            return trackerFailure.ErrorMessage;

        var execFailure = executions.FirstOrDefault(e => e.Status == SecurityTestStatus.Fail);
        if (execFailure != null)
        {
            if (!string.IsNullOrWhiteSpace(execFailure.ResultReason))
                return execFailure.ResultReason;
            if (!string.IsNullOrWhiteSpace(execFailure.ErrorMessage))
                return execFailure.ErrorMessage;
        }

        if (PatientListAuthReportHelper.TryDescribeFailure(
                capture,
                capture.LastExpectedStatus,
                capture.LastActualStatus,
                out var patientReason))
            return patientReason;

        return PatientListAuthReportHelper.SanitizeAssertionError(capture.ScenarioError);
    }

    private static string? BuildFailureSummary(
        SecurityScenarioCapture capture,
        IReadOnlyList<SecurityExecutionResult> executions,
        string? primaryReason)
    {
        if (!string.IsNullOrWhiteSpace(primaryReason))
        {
            var limit = PatientListAuthReportHelper.IsPatientListAuthScenario(capture) ? 500 : 200;
            return primaryReason.Length > limit ? primaryReason[..limit] + "..." : primaryReason;
        }

        var failedExec = executions.FirstOrDefault(e => e.Status == SecurityTestStatus.Fail);
        if (failedExec?.ExpectedStatus.HasValue == true && failedExec.ActualStatus.HasValue)
            return $"Expected {failedExec.ExpectedStatus}, actual {failedExec.ActualStatus}";

        return capture.ScenarioError;
    }

    private static SecurityExecutionResult MapTrackerResult(
        AuthorizationExecutionTracker.AuthorizationExecutionResult result,
        SecurityScenarioCapture capture,
        string suite,
        string apiBaseUrl)
    {
        var vulnerability = ExtractVulnerabilityFromLabel(result.Label)
            ?? capture.VulnerabilityType
            ?? (result.Label.Contains(' ') && !result.Label.Contains('|') ? "EndpointAccess" : "Unknown");
        var (endpointKey, method) = ParseLabelEndpoint(result.Label, capture);
        return MapSingleExecution(
            capture,
            suite,
            apiBaseUrl,
            vulnerability,
            endpointKey,
            method,
            result.ExpectedStatus,
            result.ActualStatus,
            result.ErrorMessage,
            result.Passed);
    }

    private static SecurityExecutionResult MapSingleExecution(
        SecurityScenarioCapture capture,
        string suite,
        string apiBaseUrl,
        string vulnerabilityType,
        string endpointKey,
        string httpMethod,
        int? expectedStatus,
        int? actualStatus,
        string? errorMessage,
        bool? passedOverride = null) =>
        MapSingleExecutionInternal(capture, suite, apiBaseUrl, vulnerabilityType, endpointKey, httpMethod,
            expectedStatus, actualStatus, errorMessage, passedOverride);

    private static SecurityExecutionResult MapSingleExecutionInternal(
        SecurityScenarioCapture capture,
        string suite,
        string apiBaseUrl,
        string vulnerabilityType,
        string endpointKey,
        string httpMethod,
        int? expectedStatus,
        int? actualStatus,
        string? errorMessage,
        bool? passedOverride = null)
    {
        var endpointPath = TryResolveEndpoint(endpointKey);
        var effectiveBaseUrl = ResolveApiBaseUrl(capture, apiBaseUrl);
        var fullUrl = CombineUrl(effectiveBaseUrl, endpointPath);
        var (owasp, severity, remediationKey) = SecurityOwaspMapper.Map(vulnerabilityType, suite);
        var passed = passedOverride ?? (expectedStatus.HasValue && actualStatus.HasValue && expectedStatus == actualStatus);

        return new SecurityExecutionResult
        {
            Label = $"{vulnerabilityType} | {httpMethod} {endpointKey}",
            VulnerabilityType = vulnerabilityType,
            HttpMethod = httpMethod.ToUpperInvariant(),
            EndpointPath = endpointPath,
            FullEndpointUrl = fullUrl,
            OwaspCategory = owasp,
            Severity = severity,
            Status = passed ? SecurityTestStatus.Pass : SecurityTestStatus.Fail,
            ExpectedStatus = expectedStatus,
            ActualStatus = actualStatus,
            ErrorMessage = errorMessage,
            RemediationKey = remediationKey,
            ResultReason = BuildResultReason(capture, passed, vulnerabilityType, expectedStatus, actualStatus, errorMessage)
        };
    }

    private static string? ResolveExecutionErrorMessage(
        SecurityScenarioCapture capture,
        int? expected,
        int? actual)
    {
        if (PatientListAuthReportHelper.TryDescribeFailure(capture, expected, actual, out var described))
            return described;

        return PatientListAuthReportHelper.SanitizeAssertionError(capture.ScenarioError);
    }

    private static string BuildResultReason(
        SecurityScenarioCapture capture,
        bool passed,
        string vulnerabilityType,
        int? expected,
        int? actual,
        string? errorMessage)
    {
        if (PatientListAuthReportHelper.TryDescribeFailure(capture, expected, actual, out var patientReason))
            return patientReason;

        if (passed)
        {
            return actual.HasValue
                ? $"API correctly rejected or accepted the request with HTTP {actual} for {vulnerabilityType} test (expected {expected})."
                : $"Control verified successfully for {vulnerabilityType}.";
        }

        if (expected.HasValue && actual.HasValue)
        {
            var sanitized = PatientListAuthReportHelper.SanitizeAssertionError(errorMessage);
            var suffix = string.IsNullOrWhiteSpace(sanitized) ? string.Empty : $" Details: {sanitized}";
            return $"Expected HTTP {expected}, but API returned {actual} for {vulnerabilityType}. " +
                   $"This indicates missing or weak authentication/authorization enforcement.{suffix}".Trim();
        }

        return PatientListAuthReportHelper.SanitizeAssertionError(errorMessage)
               ?? $"Security check failed for {vulnerabilityType}.";
    }

    private static string BuildNarrative(
        string stepText,
        SecurityScenarioCapture capture,
        IReadOnlyList<SecurityExecutionResult> executions)
    {
        if (stepText.Contains("API Security runs authentication test", StringComparison.OrdinalIgnoreCase))
        {
            var id = capture.TestCaseId ?? "matrix row";
            var vuln = capture.VulnerabilityType ?? "JWT mutation";
            return $"Framework loaded Excel row {id}, applied {vuln} mutation, called protected API, compared status code.";
        }

        if (stepText.Contains("Authorization test runs for endpoint", StringComparison.OrdinalIgnoreCase))
        {
            return $"Logged in as role '{capture.Role}', called {capture.HttpMethod} {capture.EndpointKey}, validated RBAC status.";
        }

        if (stepText.Contains("Authorization executes all", StringComparison.OrdinalIgnoreCase))
        {
            return $"Executed all enabled Excel rows ({executions.Count} checks recorded in this scenario).";
        }

        if (stepText.Contains("valid access token", StringComparison.OrdinalIgnoreCase))
            return "Baseline login on Auth host; valid JWT stored for subsequent mutation.";

        if (stepText.Contains("sends flexible", StringComparison.OrdinalIgnoreCase)
            && stepText.Contains("patientList", StringComparison.OrdinalIgnoreCase))
        {
            var tokenMode = FlexibleTokenModeFromStep(stepText);
            return tokenMode switch
            {
                "none" => "POST Patient List without Authorization header; expects 401.",
                "empty" => "POST Patient List with Authorization: Bearer (empty); expects 401.",
                "garbage" => "POST Patient List with garbage JWT (abc.def.ghi); expects 401.",
                "malformed" => "POST Patient List with malformed JWT; expects 401.",
                "current" => $"POST Patient List using mutated JWT from prior step ({capture.VulnerabilityType ?? "token mutation"}); expects 401.",
                _ => "POST Patient List with configured auth token mode; expects 401."
            };
        }

        if (stepText.Contains("expired access token", StringComparison.OrdinalIgnoreCase))
            return "Valid JWT replaced with expired token before Patient List POST.";

        if (stepText.Contains("tampered access token", StringComparison.OrdinalIgnoreCase))
            return "Valid JWT captured, then signature/payload tampered for negative test.";

        if (stepText.Contains("wrong issuer audience access token", StringComparison.OrdinalIgnoreCase))
            return "Valid JWT replaced with wrong issuer/audience before Patient List POST.";

        if (stepText.Contains("missing claim access token", StringComparison.OrdinalIgnoreCase))
            return "Valid JWT stripped of a required claim before Patient List POST.";

        if (stepText.Contains("all authorization executions should pass", StringComparison.OrdinalIgnoreCase))
            return "Soft-assert aggregator verified all row-level executions passed.";

        if (stepText.Contains("Authorization status code should be", StringComparison.OrdinalIgnoreCase))
            return "Validated HTTP status from last authorization API call against expected matrix value.";

        if (stepText.Contains("the API status code should be", StringComparison.OrdinalIgnoreCase)
            || stepText.Contains("Status code should be", StringComparison.OrdinalIgnoreCase))
            return "Validated HTTP status from last API response against expected value.";

        if (stepText.StartsWith("When User sends", StringComparison.OrdinalIgnoreCase))
            return $"Sent {capture.HttpMethod ?? "HTTP"} request to {capture.EndpointKey ?? "configured endpoint"}.";

        return "Step executed as defined in Gherkin scenario.";
    }

    private static string? FlexibleTokenModeFromStep(string stepText)
    {
        var match = Regex.Match(stepText, @"token ""([^""]+)""", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim().ToLowerInvariant() : null;
    }

    private static string InferStepType(string stepText)
    {
        if (stepText.StartsWith("Given ", StringComparison.OrdinalIgnoreCase)) return "Given";
        if (stepText.StartsWith("When ", StringComparison.OrdinalIgnoreCase)) return "When";
        if (stepText.StartsWith("Then ", StringComparison.OrdinalIgnoreCase)) return "Then";
        if (stepText.StartsWith("And ", StringComparison.OrdinalIgnoreCase)) return "And";
        return "Step";
    }

    private static string? ExtractVulnerabilityFromLabel(string label)
    {
        var parts = label.Split('|', StringSplitOptions.TrimEntries);
        return parts.Length >= 2 ? parts[1] : null;
    }

    private static (string EndpointKey, string Method) ParseLabelEndpoint(
        string label,
        SecurityScenarioCapture capture)
    {
        var parts = label.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length >= 3)
        {
            var methodEndpoint = parts[2].Split(' ', 2, StringSplitOptions.TrimEntries);
            if (methodEndpoint.Length == 2)
                return (methodEndpoint[1], methodEndpoint[0]);
        }

        // Authorization matrix format: "SuperAdmin GET get"
        var tokens = label.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length >= 3)
            return (tokens[2], tokens[1]);

        return (capture.EndpointKey ?? "get", capture.HttpMethod ?? "GET");
    }

    private static string TryResolveEndpoint(string endpointKey)
    {
        try
        {
            return ExcelConfigReader.GetEndpoint(endpointKey);
        }
        catch
        {
            return endpointKey;
        }
    }

    private static string ResolveApiBaseUrl(SecurityScenarioCapture capture, string defaultBaseUrl)
    {
        if (PatientListAuthReportHelper.IsPatientListAuthScenario(capture)
            && !string.IsNullOrWhiteSpace(AppConfiguration.ApiUrls.ApimBaseUrl))
        {
            return AppConfiguration.ApiUrls.ApimBaseUrl;
        }

        return defaultBaseUrl;
    }

    private static string CombineUrl(string baseUrl, string endpointPath)
    {
        var baseNormalized = baseUrl.TrimEnd('/');
        var path = endpointPath.StartsWith('/') ? endpointPath : "/" + endpointPath;
        return baseNormalized + path;
    }

    private static string? ExtractEndpointFromTags(IReadOnlyList<string> tags)
    {
        foreach (var tag in tags)
        {
            var match = ApiPathTagRegex.Match(tag.Trim());
            if (match.Success)
                return match.Groups[1].Value;
        }

        return null;
    }
}

/// <summary>Mutable capture DTO used by hooks before enrichment.</summary>
public sealed class SecurityScenarioCapture
{
    public required string FeatureName { get; init; }
    public required string ScenarioName { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required IReadOnlyList<string> StepTexts { get; init; }
    public required IReadOnlyList<AuthorizationExecutionTracker.AuthorizationExecutionResult> TrackerResults { get; init; }
    public bool OverallFailed { get; init; }
    public string? ScenarioError { get; init; }
    public TimeSpan Duration { get; init; }
    public int? LastExpectedStatus { get; init; }
    public int? LastActualStatus { get; init; }

    public string? TestCaseId { get; set; }
    public string? VulnerabilityType { get; set; }
    public string? EndpointKey { get; set; }
    public string? HttpMethod { get; set; }
    public string? Role { get; set; }
}
