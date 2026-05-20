using System.Collections.Concurrent;
using System.Net;
using System.Text;
using AventStack.ExtentReports;
using AventStack.ExtentReports.Gherkin.Model;
using EnterpriseApiAutomationFramework.Core.Reporting;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;

namespace EnterpriseApiAutomationFramework.Hooks;

[Binding]
public class ExtentReportHooks
{
    private static readonly ConcurrentDictionary<string, ExtentTest> FeatureNodes = new();

    private ExtentTest? _scenarioNode;
    private DateTime _scenarioStartUtc;
    private DateTime _stepStartUtc;

    private readonly ScenarioContext _scenarioContext;
    private readonly FeatureContext _featureContext;

    public ExtentReportHooks(ScenarioContext scenarioContext, FeatureContext featureContext)
    {
        _scenarioContext = scenarioContext;
        _featureContext = featureContext;
    }

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        ExtentReportManager.Initialize();
    }

    [AfterTestRun]
    public static void AfterTestRun()
    {
        ExtentReportManager.Flush();
    }

    [BeforeFeature]
    public static void BeforeFeature(FeatureContext featureContext)
    {
        var title = featureContext.FeatureInfo.Title;
        FeatureNodes.GetOrAdd(
            title,
            _ => ExtentReportManager.Instance
                .CreateTest<Feature>(title)
                .AssignCategory(featureContext.FeatureInfo.Tags.ToArray()));
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        _scenarioStartUtc = DateTime.UtcNow;
        var featureTitle = _featureContext.FeatureInfo.Title;
        var scenarioTitle = _scenarioContext.ScenarioInfo.Title;

        _scenarioNode = FeatureNodes[featureTitle]
            .CreateNode<Scenario>(scenarioTitle)
            .AssignCategory(_scenarioContext.ScenarioInfo.Tags.ToArray());
    }

    [BeforeStep]
    public void BeforeStep()
    {
        ReportExecutionContext.BeginStep();
        _stepStartUtc = DateTime.UtcNow;
    }

    [AfterStep]
    public void AfterStep()
    {
        var stepInfo = _scenarioContext.StepContext.StepInfo;
        var stepDurationMs = (DateTime.UtcNow - _stepStartUtc).TotalMilliseconds;
        var apiCalls = ReportExecutionContext.GetStepApiCalls();
        var expectedMs = ReportExecutionContext.ExpectedResponseTimeMs;

        var stepNode = CreateStepNode(_scenarioNode!, stepInfo.StepDefinitionType, stepInfo.Text);
        var details = BuildStepDetails(stepInfo.Text, stepDurationMs, apiCalls, expectedMs);
        stepNode.Info(details);

        var hasSlowApi = apiCalls.Any(c => c.ExceedsThreshold(expectedMs));
        var stepFailed = _scenarioContext.TestError != null;

        if (stepFailed)
            stepNode.Fail(_scenarioContext.TestError!.Message);
        else if (hasSlowApi)
            stepNode.Warning("One or more API calls exceeded the expected response time threshold.");
        else
            stepNode.Pass("Step completed successfully.");

        ReportExecutionContext.ClearStep();
    }

    [AfterScenario]
    public void AfterScenario()
    {
        var scenarioDurationMs = (DateTime.UtcNow - _scenarioStartUtc).TotalMilliseconds;
        var summary =
            $"<b>Overall Scenario Duration:</b> {scenarioDurationMs:F0} ms";

        _scenarioNode!.Info(summary);

        if (_scenarioContext.TestError != null)
            _scenarioNode.Fail(_scenarioContext.TestError.Message);
        else
            _scenarioNode.Pass("Scenario passed.");
    }

    private static ExtentTest CreateStepNode(
        ExtentTest scenario,
        StepDefinitionType stepDefinitionType,
        string stepText) =>
        stepDefinitionType switch
        {
            StepDefinitionType.Given => scenario.CreateNode<Given>(stepText),
            StepDefinitionType.When => scenario.CreateNode<When>(stepText),
            StepDefinitionType.Then => scenario.CreateNode<Then>(stepText),
            _ => scenario.CreateNode<When>(stepText)
        };

    private static string BuildStepDetails(
        string stepName,
        double stepDurationMs,
        IReadOnlyList<ApiCallRecord> apiCalls,
        long expectedMs)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<b>Step:</b> {WebUtility.HtmlEncode(stepName)}");
        sb.AppendLine($"<b>Step Execution Time:</b> {stepDurationMs:F0} ms");

        if (apiCalls.Count == 0)
            return sb.ToString();

        foreach (var call in apiCalls)
        {
            var exceeded = call.ExceedsThreshold(expectedMs);
            var timeColor = exceeded ? "#c0392b" : "#27ae60";
            var timeStatus = exceeded ? "EXCEEDS THRESHOLD" : "WITHIN THRESHOLD";

            sb.AppendLine("<hr/>");
            sb.AppendLine($"<b>API:</b> {WebUtility.HtmlEncode(call.Method)} {WebUtility.HtmlEncode(call.Endpoint)}");
            sb.AppendLine($"<b>Status Code:</b> {call.StatusCode}");
            sb.AppendLine(
                $"<b>API Response Time:</b> <span style='color:{timeColor};font-weight:bold'>{call.ElapsedMilliseconds} ms</span> " +
                $"(Expected: &lt; {expectedMs} ms) — <span style='color:{timeColor}'>{timeStatus}</span>");

            if (!string.IsNullOrWhiteSpace(call.RequestPayload))
            {
                sb.AppendLine("<b>Request Payload:</b>");
                sb.AppendLine(
                    $"<pre class='api-report-pre'>{WebUtility.HtmlEncode(call.RequestPayload)}</pre>");
            }

            if (!string.IsNullOrWhiteSpace(call.ResponseBody))
            {
                sb.AppendLine("<b>Response Body:</b>");
                sb.AppendLine(
                    $"<pre class='api-report-pre'>{WebUtility.HtmlEncode(call.ResponseBody)}</pre>");
            }
        }

        return sb.ToString();
    }
}
