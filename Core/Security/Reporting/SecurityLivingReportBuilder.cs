using EnterpriseApiAutomationFramework.Core.ParallelExecution.Reporting;



namespace EnterpriseApiAutomationFramework.Core.Security.Reporting;



public sealed class SecurityLivingReportBuilder

{

    public SecurityLivingReport Build(SecurityRunSnapshot snapshot)

    {

        var scenarios = new List<SecurityScenarioReport>();



        foreach (var raw in snapshot.Scenarios)
        {
            if (raw.Executions.Count == 0 && raw.OverallStatus != SecurityTestStatus.Fail)
                continue;

            scenarios.Add(raw);
        }



        var dashboard = scenarios

            .SelectMany(s => s.Executions.Select(e => new SecurityExecutionResult
            {
                Label = $"{s.ScenarioName} :: {e.Label}",
                ScenarioName = s.ScenarioName,
                VulnerabilityType = e.VulnerabilityType,
                HttpMethod = e.HttpMethod,
                EndpointPath = e.EndpointPath,
                FullEndpointUrl = e.FullEndpointUrl,
                OwaspCategory = e.OwaspCategory,
                Severity = e.Severity,
                Status = e.Status,
                ExpectedStatus = e.ExpectedStatus,
                ActualStatus = e.ActualStatus,
                ResultReason = e.ResultReason,
                ErrorMessage = e.ErrorMessage,
                RemediationKey = e.RemediationKey,
                FailedStepText = e.Status == SecurityTestStatus.Fail ? s.FailedStepText : null,
                FailureSummary = e.Status == SecurityTestStatus.Fail
                    ? s.FailureSummary ?? e.ResultReason ?? e.ErrorMessage
                    : null
            }))

            .ToList();



        return new SecurityLivingReport

        {

            Metadata = snapshot.Metadata,

            Scenarios = scenarios,

            DashboardRows = dashboard

        };

    }



    public static (string MarkdownPath, string HtmlPath) BuildAndRender(string? runFilePath = null)

    {

        if (!SecurityReportCollector.IsEnabled)

            throw new InvalidOperationException("Security reporting is disabled in appsettings.json.");



        var snapshot = SecurityReportCollector.LoadRun(runFilePath)

            ?? throw new InvalidOperationException("No security run snapshot found to build report.");



        if (snapshot.Scenarios.Count == 0 && !string.IsNullOrWhiteSpace(snapshot.Metadata.TrxFilePath))

        {

            Console.WriteLine("Security report: no scenarios captured in JSON; TRX-only mode not implemented for empty runs.");

        }



        var builder = new SecurityLivingReportBuilder();

        var report = builder.Build(snapshot);



        var settings = SecurityReportCollector.SecurityReportingSettings;

        var outputDir = SecurityReportCollector.ResolvePath(settings.OutputPath);

        Directory.CreateDirectory(outputDir);



        var baseName = $"security-living-report_{snapshot.Metadata.RunId}";

        var mdFileName = baseName + ".md";

        var htmlFileName = baseName + ".html";

        var mdPath = Path.Combine(outputDir, mdFileName);

        var htmlPath = Path.Combine(outputDir, htmlFileName);



        var markdown = SecurityLivingReportRenderer.RenderMarkdown(report, htmlFileName);

        var html = SecurityLivingReportRenderer.RenderHtml(report, htmlFileName);



        File.WriteAllText(mdPath, markdown);

        File.WriteAllText(htmlPath, html);



        if (settings.GenerateLatestCopy)

        {

            File.WriteAllText(Path.Combine(outputDir, "latest.html"), html);

            File.WriteAllText(Path.Combine(outputDir, "latest.md"), markdown);

        }



        var jsonFileName = $"run_{snapshot.Metadata.RunId}.json";

        SecurityRunManifest.AppendEntry(new SecurityRunManifestEntry
        {
            RunId = snapshot.Metadata.RunId,
            ExecutedAtUtc = snapshot.Metadata.ExecutedAtUtc,
            HtmlFile = htmlFileName,
            MdFile = mdFileName,
            JsonFile = jsonFileName,
            TrxFile = snapshot.Metadata.TrxFilePath,
            RunMode = snapshot.Metadata.RunMode,
            TestFilter = snapshot.Metadata.TestFilter,
            TotalScenarios = report.TotalScenarios,
            PassedScenarios = report.PassedScenarios,
            FailedScenarios = report.FailedScenarios
        });



        if (settings.GenerateRunIndex)

        {

            var indexHtml = SecurityLivingReportRenderer.RenderRunIndexHtml(SecurityRunManifest.LoadAll());

            File.WriteAllText(Path.Combine(outputDir, "index.html"), indexHtml);

        }



        SecurityReportCleanup.ApplyRetentionIfConfigured();



        Console.WriteLine($"Security Living Report (HTML)   : {htmlPath}");

        Console.WriteLine($"Security Living Report (Markdown): {mdPath}");

        if (settings.GenerateLatestCopy)

            Console.WriteLine($"Latest shortcut                  : {Path.Combine(outputDir, "latest.html")}");

        if (settings.GenerateRunIndex)

            Console.WriteLine($"Run history index                : {Path.Combine(outputDir, "index.html")}");



        return (mdPath, htmlPath);

    }



    public static string? FindLatestTrxFile()

    {

        var candidates = new[]

        {

            SecurityReportCollector.ResolvePath("TestResults/security-results.trx"),

            SecurityReportCollector.ResolvePath("TestResults/test-results.trx")

        };



        return candidates

            .Where(File.Exists)

            .OrderByDescending(File.GetLastWriteTimeUtc)

            .FirstOrDefault();

    }

}


