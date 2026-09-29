namespace EnterpriseApiAutomationFramework.Core.Security.Reporting;

public enum SecuritySeverity
{
    Critical,
    High,
    Medium,
    Low,
    Informational
}

public enum SecurityTestStatus
{
    Pass,
    Fail,
    Skipped
}

public sealed class SecurityReportingSettings
{
    public bool Enabled { get; init; } = true;
    public string OutputPath { get; init; } = "Reports/Security/LivingReport";
    public string RunsPath { get; init; } = "Reports/Security/runs";
    public bool GenerateLatestCopy { get; init; } = true;
    public bool GenerateRunIndex { get; init; } = true;
    public int RetentionDays { get; init; } = 90;
}

public sealed class SecurityRunManifestEntry
{
    public required string RunId { get; init; }
    public required DateTimeOffset ExecutedAtUtc { get; init; }
    public required string HtmlFile { get; init; }
    public required string MdFile { get; init; }
    public required string JsonFile { get; init; }
    public string? TrxFile { get; init; }
    public string RunMode { get; init; } = SecurityReportingConstants.RunModeBatch;
    public string? TestFilter { get; init; }
    public int TotalScenarios { get; init; }
    public int PassedScenarios { get; init; }
    public int FailedScenarios { get; init; }
}

public sealed class SecurityRunMetadata
{
    public required string RunId { get; init; }
    public required string ProjectName { get; init; }
    public required string Environment { get; init; }
    public required string ApiBaseUrl { get; init; }
    public required string AuthBaseUrl { get; init; }
    public required string Framework { get; init; }
    public required DateTimeOffset ExecutedAtUtc { get; init; }
    public string? TrxFilePath { get; init; }
    public string RunMode { get; init; } = SecurityReportingConstants.RunModeBatch;
    public string? TestFilter { get; init; }
    public DateTimeOffset? CompletedAtUtc { get; init; }
    public TimeSpan Duration { get; init; }
    public int SkippedScenarios { get; init; }
}

public sealed class SecurityStepExecution
{
    public required string StepText { get; init; }
    public required string StepType { get; init; }
    public required string Narrative { get; init; }
    public SecurityTestStatus Status { get; init; }
    public string? FailureReason { get; init; }
}

public sealed class SecurityExecutionResult
{
    public required string Label { get; init; }
    public required string VulnerabilityType { get; init; }
    public required string HttpMethod { get; init; }
    public required string EndpointPath { get; init; }
    public required string FullEndpointUrl { get; init; }
    public required string OwaspCategory { get; init; }
    public SecuritySeverity Severity { get; init; }
    public SecurityTestStatus Status { get; init; }
    public int? ExpectedStatus { get; init; }
    public int? ActualStatus { get; init; }
    public string? ResultReason { get; init; }
    public string? ErrorMessage { get; init; }
    public string? RemediationKey { get; init; }
    public string? ScenarioName { get; init; }
    public string? FailedStepText { get; init; }
    public string? FailureSummary { get; init; }
}

public sealed class SecurityScenarioReport
{
    public required string FeatureName { get; init; }
    public required string ScenarioName { get; init; }
    public required string Suite { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required IReadOnlyList<SecurityStepExecution> Steps { get; init; }
    public required IReadOnlyList<SecurityExecutionResult> Executions { get; init; }
    public SecurityTestStatus OverallStatus { get; init; }
    public string? ScenarioError { get; init; }
    public TimeSpan Duration { get; init; }
    public string? FailedStepText { get; init; }
    public string? FailureSummary { get; init; }
}

public sealed class SecurityRemediationEntry
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required string VulnerableCode { get; init; }
    public required string SecuredCode { get; init; }
}

public sealed class SecurityLivingReport
{
    public required SecurityRunMetadata Metadata { get; init; }
    public required IReadOnlyList<SecurityScenarioReport> Scenarios { get; init; }
    public required IReadOnlyList<SecurityExecutionResult> DashboardRows { get; init; }
    public int TotalScenarios => Scenarios.Count;
    public int PassedScenarios => Scenarios.Count(s => s.OverallStatus == SecurityTestStatus.Pass);
    public int FailedScenarios => Scenarios.Count(s => s.OverallStatus == SecurityTestStatus.Fail);
    public int TotalExecutions => DashboardRows.Count;
    public int PassedExecutions => DashboardRows.Count(r => r.Status == SecurityTestStatus.Pass);
    public int FailedExecutions => DashboardRows.Count(r => r.Status == SecurityTestStatus.Fail);
}

public sealed class SecurityRunSnapshot
{
    public required SecurityRunMetadata Metadata { get; init; }
    public required List<SecurityScenarioReport> Scenarios { get; init; }
}
