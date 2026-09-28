namespace EnterpriseApiAutomationFramework.Core.Security.Reporting;

public static class SecurityReportingConstants
{
    public const string LastExpectedStatusKey = "SecurityLastExpectedStatus";

    public const string SessionIdEnvVar = "SECURITY_RUN_SESSION_ID";
    public const string DeferRenderEnvVar = "SECURITY_DEFER_RENDER";
    public const string TestFilterEnvVar = "SECURITY_TEST_FILTER";

    public const string RunModeSingleScenario = "SingleScenario";
    public const string RunModeBatch = "Batch";
    public const string RunModeFullSuite = "FullSuite";
    public const string RunModeParallelBatch = "ParallelBatch";

    public const int FullSuiteScenarioThreshold = 30;
}
