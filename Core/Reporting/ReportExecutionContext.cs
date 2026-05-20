using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Reporting;

/// <summary>
/// Tracks API calls per SpecFlow step using AsyncLocal for parallel-safe execution.
/// </summary>
public static class ReportExecutionContext
{
    private static readonly AsyncLocal<List<ApiCallRecord>> StepApiCalls = new();
    private static long _expectedResponseTimeMs = 3000;

    public static long ExpectedResponseTimeMs => _expectedResponseTimeMs;

    public static void Configure()
    {
        var configured = ConfigReader.GetValue("ExpectedResponseTimeMs");
        if (long.TryParse(configured, out var ms) && ms > 0)
            _expectedResponseTimeMs = ms;
    }

    public static void BeginStep() => StepApiCalls.Value = new List<ApiCallRecord>();

    public static void RecordApiCall(ApiCallRecord record)
    {
        StepApiCalls.Value ??= new List<ApiCallRecord>();
        StepApiCalls.Value.Add(record);
    }

    public static IReadOnlyList<ApiCallRecord> GetStepApiCalls() =>
        StepApiCalls.Value ?? (IReadOnlyList<ApiCallRecord>)Array.Empty<ApiCallRecord>();

    public static void ClearStep() => StepApiCalls.Value = new List<ApiCallRecord>();
}
