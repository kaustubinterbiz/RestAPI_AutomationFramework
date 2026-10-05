using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Authorization;
using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Core.Security.Reporting;
using NUnit.Framework;
using Reqnroll;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Hooks;

[Binding]
public class SecurityReportHooks
{
    public const string CapturedStepsKey = "SecurityReportCapturedSteps";

    private readonly ScenarioContext _scenarioContext;
    private readonly FeatureContext _featureContext;

    public SecurityReportHooks(ScenarioContext scenarioContext, FeatureContext featureContext)
    {
        _scenarioContext = scenarioContext;
        _featureContext = featureContext;
    }

    [BeforeScenario(Order = 1)]
    public void BeforeSecurityScenario()
    {
        if (!SecurityReportCollector.IsEnabled)
            return;

        _scenarioContext.Set(new List<string>(), CapturedStepsKey);
    }

    [AfterStep(Order = 9000)]
    public void AfterSecurityStep()
    {
        if (!SecurityReportCollector.IsEnabled)
            return;

        if (!_scenarioContext.TryGetValue(CapturedStepsKey, out List<string> steps))
        {
            steps = new List<string>();
            _scenarioContext.Set(steps, CapturedStepsKey);
        }

        var stepInfo = _scenarioContext.StepContext.StepInfo;
        steps.Add($"{stepInfo.StepDefinitionType} {stepInfo.Text}");
    }

    [BeforeTestRun(Order = 5)]
    public static void BeforeSecurityTestRun()
    {
        SecurityReportCollector.BeginRun();
    }

    [AfterTestRun(Order = 9999)]
    public static void AfterSecurityTestRun()
    {
        if (!SecurityReportCollector.IsEnabled)
            return;

        try
        {
            var trx = SecurityLivingReportBuilder.FindLatestTrxFile();
            SecurityReportCollector.SetTrxFilePath(trx);
            SecurityReportCollector.FinalizeRun();

            if (!SecurityReportCollector.TryRenderReport(out var skipReason))
            {
                if (!string.IsNullOrWhiteSpace(skipReason))
                    Console.WriteLine($"Security living report: {skipReason}");
                return;
            }

            SecurityLivingReportBuilder.BuildAndRender();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Security living report generation skipped: {ex.Message}");
        }
    }

    [AfterScenario(Order = 9000)]
    public void AfterSecurityScenario()
    {
        if (!SecurityReportCollector.IsEnabled)
            return;

        var tags = _scenarioContext.ScenarioInfo.Tags.ToList();
        var suite = ResolveSuite(tags, _scenarioContext.ScenarioInfo.Title);

        var trackerResults = AuthorizationExecutionTracker.GetResults(_scenarioContext).ToList();
        var (lastExpected, lastActual) = ResolveLastStatusCodes();
        var stepTexts = _scenarioContext.TryGetValue(CapturedStepsKey, out List<string> capturedSteps)
            ? capturedSteps
            : new List<string>();

        if (!lastExpected.HasValue)
            lastExpected = SecurityScenarioEnricher.InferExpectedStatusFromSteps(stepTexts);

        var testResult = TestContext.CurrentContext.Result.Outcome.Status;
        var overallFailed = testResult == NUnit.Framework.Interfaces.TestStatus.Failed;
        var rawError = overallFailed ? TestContext.CurrentContext.Result.Message : null;

        var capture = new SecurityScenarioCapture
        {
            FeatureName = _featureContext.FeatureInfo.Title,
            ScenarioName = _scenarioContext.ScenarioInfo.Title,
            Tags = tags,
            StepTexts = stepTexts,
            TrackerResults = trackerResults,
            OverallFailed = overallFailed,
            ScenarioError = SanitizeScenarioError(rawError, stepTexts, _scenarioContext.ScenarioInfo.Title, lastExpected, lastActual),
            Duration = TimeSpan.Zero,
            LastExpectedStatus = lastExpected,
            LastActualStatus = lastActual
        };

        SecurityScenarioEnricher.InferMetadataFromSteps(capture);

        var apiBaseUrl = AppConfiguration.ApiUrls.ApiBaseUrl;
        var executions = SecurityScenarioEnricher.BuildExecutions(capture, suite, apiBaseUrl);
        var steps = SecurityScenarioEnricher.BuildStepNarratives(capture, executions);
        var (failedStepText, failureSummary) = SecurityScenarioEnricher.ResolveFailureDetails(capture, executions, steps);

        var overallStatus = overallFailed
            ? SecurityTestStatus.Fail
            : executions.Any(HasRealStatusMismatch)
                ? SecurityTestStatus.Fail
                : SecurityTestStatus.Pass;

        var scenarioReport = new SecurityScenarioReport
        {
            FeatureName = capture.FeatureName,
            ScenarioName = capture.ScenarioName,
            Suite = suite,
            Tags = capture.Tags,
            Steps = steps,
            Executions = executions,
            OverallStatus = overallStatus,
            ScenarioError = capture.ScenarioError,
            Duration = capture.Duration,
            FailedStepText = failedStepText,
            FailureSummary = failureSummary
        };

        SecurityReportCollector.RecordScenario(scenarioReport);
    }

    private (int? Expected, int? Actual) ResolveLastStatusCodes()
    {
        int? lastExpected = null;
        int? lastActual = null;

        if (_scenarioContext.TryGetValue(AuthorizationConstants.LastExpectedStatusKey, out int authExpected))
            lastExpected = authExpected;
        else if (_scenarioContext.TryGetValue(SecurityReportingConstants.LastExpectedStatusKey, out int securityExpected))
            lastExpected = securityExpected;

        if (_scenarioContext.TryGetValue(AuthorizationConstants.LastResponseKey, out RestResponse authResponse))
            lastActual = (int)authResponse.StatusCode;
        else if (_scenarioContext.TryGetValue(TokenContext.LastResponseKey, out RestResponse tokenResponse))
            lastActual = (int)tokenResponse.StatusCode;

        return (lastExpected, lastActual);
    }

    private static string? SanitizeScenarioError(
        string? rawError,
        IReadOnlyList<string> stepTexts,
        string scenarioName,
        int? expected,
        int? actual)
    {
        if (string.IsNullOrWhiteSpace(rawError))
            return null;

        var capture = new SecurityScenarioCapture
        {
            FeatureName = string.Empty,
            ScenarioName = scenarioName,
            Tags = Array.Empty<string>(),
            StepTexts = stepTexts,
            TrackerResults = Array.Empty<AuthorizationExecutionTracker.AuthorizationExecutionResult>(),
            OverallFailed = true,
            Duration = TimeSpan.Zero
        };

        PatientListAuthReportHelper.TryEnrichMetadata(capture);
        if (PatientListAuthReportHelper.TryDescribeFailure(capture, expected, actual, out var described))
            return described;

        return PatientListAuthReportHelper.SanitizeAssertionError(rawError);
    }

    private static bool HasRealStatusMismatch(SecurityExecutionResult execution) =>
        execution.Status == SecurityTestStatus.Fail
        && execution.ExpectedStatus.HasValue
        && execution.ActualStatus.HasValue
        && execution.ExpectedStatus != execution.ActualStatus;

    private static string ResolveSuite(IReadOnlyList<string> tags, string scenarioName)
    {
        var isPatientSecurity = tags.Any(t => t.Equals("Patient", StringComparison.OrdinalIgnoreCase))
            || PatientListAuthReportHelper.IsPatScenarioName(scenarioName)
            || tags.Any(t => t.Contains("api/v2/Patient", StringComparison.OrdinalIgnoreCase));

        if (isPatientSecurity)
        {
            if (tags.Any(t => t.Equals("IDOR", StringComparison.OrdinalIgnoreCase)))
                return "Patient Security / IDOR";
            if (tags.Any(t => t.Equals("InputValidation", StringComparison.OrdinalIgnoreCase)))
                return "Patient Security / InputValidation";
            return "Patient Security / Authentication";
        }

        if (tags.Any(t => t.Equals("Security", StringComparison.OrdinalIgnoreCase)))
            return "Authentication";

        if (tags.Any(t => t.Equals("Authorization", StringComparison.OrdinalIgnoreCase)))
            return "Authorization";

        return "Functional";
    }
}
