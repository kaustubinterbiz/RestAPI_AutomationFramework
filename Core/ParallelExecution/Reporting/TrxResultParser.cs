using System.Xml.Linq;
using EnterpriseApiAutomationFramework.Core.ParallelExecution.Models;

namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Reporting;

public static class TrxResultParser
{
    public static IReadOnlyList<ScenarioExecutionResult> Parse(string? trxFilePath)
    {
        if (string.IsNullOrWhiteSpace(trxFilePath) || !File.Exists(trxFilePath))
            return Array.Empty<ScenarioExecutionResult>();

        try
        {
            var document = XDocument.Load(trxFilePath);
            var ns = document.Root?.Name.Namespace ?? XNamespace.None;

            return document
                .Descendants(ns + "UnitTestResult")
                .Select(element => MapScenario(element, ns))
                .ToList();
        }
        catch
        {
            return Array.Empty<ScenarioExecutionResult>();
        }
    }

    private static ScenarioExecutionResult MapScenario(XElement element, XNamespace ns)
    {
        var name = element.Attribute("testName")?.Value ?? "Unknown Scenario";
        var outcome = element.Attribute("outcome")?.Value ?? "Failed";
        var durationText = element.Attribute("duration")?.Value;
        var duration = TimeSpan.TryParse(durationText, out var parsed) ? parsed : TimeSpan.Zero;

        var output = element.Element(ns + "Output");
        var errorInfo = output?.Element(ns + "ErrorInfo");
        var message = errorInfo?.Element(ns + "Message")?.Value;
        var stackTrace = errorInfo?.Element(ns + "StackTrace")?.Value;

        var status = outcome.ToUpperInvariant() switch
        {
            "PASSED" => ExecutionStatus.Success,
            "SKIPPED" or "NOTEXECUTED" or "IGNORED" => ExecutionStatus.Skipped,
            _ => ExecutionStatus.Failed
        };

        return new ScenarioExecutionResult
        {
            ScenarioName = name,
            Status = status,
            Duration = duration,
            ErrorMessage = message,
            StackTrace = stackTrace
        };
    }
}
