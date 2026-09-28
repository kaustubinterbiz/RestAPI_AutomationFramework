using System.Net;
using System.Text;

namespace EnterpriseApiAutomationFramework.Core.Security.Reporting;

public static class SecurityLivingReportRenderer
{
    public static string RenderMarkdown(SecurityLivingReport report, string? reportFileName = null)
    {
        var sb = new StringBuilder();
        var meta = report.Metadata;
        var localTime = meta.ExecutedAtUtc.ToLocalTime();

        sb.AppendLine("# API Security Testing Living Documentation Report");
        sb.AppendLine();
        sb.AppendLine("## 1. Executive Summary & Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| **Project Name** | {meta.ProjectName} |");
        sb.AppendLine($"| **Execution Date (UTC)** | {meta.ExecutedAtUtc:yyyy-MM-dd HH:mm:ss} UTC |");
        sb.AppendLine($"| **Execution Date (Local)** | {localTime:yyyy-MM-dd HH:mm:ss} |");
        sb.AppendLine($"| **Target Environment** | {meta.Environment} |");
        sb.AppendLine($"| **API Base URL** | {meta.ApiBaseUrl} |");
        sb.AppendLine($"| **Framework** | {meta.Framework} |");
        if (!string.IsNullOrWhiteSpace(reportFileName))
            sb.AppendLine($"| **Report File** | `{reportFileName}` |");
        sb.AppendLine($"| **Scenarios** | {report.PassedScenarios} passed / {report.FailedScenarios} failed / {report.TotalScenarios} total |");
        sb.AppendLine($"| **Execution Checks** | {report.PassedExecutions} passed / {report.FailedExecutions} failed / {report.TotalExecutions} total |");
        sb.AppendLine();

        sb.AppendLine("### Summary Table");
        sb.AppendLine();
        sb.AppendLine("| Endpoint | HTTP Method | Feature / Vulnerability | Scenario Name | OWASP Category | Status | Risk Severity |");
        sb.AppendLine("|---|---|---|---|---|---|---|");

        foreach (var row in report.DashboardRows)
        {
            var scenarioName = row.Label.Contains(" :: ")
                ? row.Label.Split(" :: ", 2)[0]
                : row.Label;

            sb.AppendLine(
                $"| `{row.FullEndpointUrl}` | {row.HttpMethod} | {row.VulnerabilityType} | {EscapePipe(scenarioName)} | {row.OwaspCategory} | **{row.Status}** | {SecurityOwaspMapper.FormatSeverity(row.Severity)} |");

            if (row.Status == SecurityTestStatus.Fail)
            {
                var reason = row.FailureSummary ?? row.ResultReason ?? row.ErrorMessage ?? "—";
                sb.AppendLine($"  - Failed step: `{row.FailedStepText ?? "—"}` | Reason: {EscapePipe(reason)} | Expected/Actual: {row.ExpectedStatus}/{row.ActualStatus}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 2. Detailed Feature & Scenario Breakdown");
        sb.AppendLine();

        foreach (var scenario in report.Scenarios)
        {
            RenderScenarioMarkdown(sb, scenario);
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 3. Actionable Remediation for Developers (.NET C#)");
        sb.AppendLine();

        var failedExecutions = report.Scenarios
            .SelectMany(s => s.Executions)
            .Where(e => e.Status == SecurityTestStatus.Fail)
            .ToList();

        if (failedExecutions.Count == 0)
        {
            sb.AppendLine("No failed scenarios — no remediation required for this run.");
        }
        else
        {
            var remediations = SecurityRemediationCatalog.GetForFailedExecutions(failedExecutions);
            foreach (var remediation in remediations)
            {
                sb.AppendLine($"### {remediation.Title}");
                sb.AppendLine();
                sb.AppendLine("#### Vulnerable C# ASP.NET Core Code");
                sb.AppendLine("```csharp");
                sb.AppendLine(remediation.VulnerableCode.TrimEnd());
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("#### Secured C# ASP.NET Core Code");
                sb.AppendLine("```csharp");
                sb.AppendLine(remediation.SecuredCode.TrimEnd());
                sb.AppendLine("```");
                sb.AppendLine();
            }

            sb.AppendLine("### Failed Scenario Remediation Map");
            sb.AppendLine();
            foreach (var fail in failedExecutions)
            {
                var remediation = SecurityRemediationCatalog.Get(fail.RemediationKey);
                sb.AppendLine($"- **{fail.Label}** → {remediation.Title}");
            }
        }

        return sb.ToString();
    }

    public static string RenderHtml(SecurityLivingReport report, string? reportFileName = null)
    {
        var meta = report.Metadata;
        var localTime = meta.ExecutedAtUtc.ToLocalTime();
        reportFileName ??= $"security-living-report_{meta.RunId}.html";
        var successRate = report.TotalExecutions == 0
            ? 100.0
            : Math.Round(100.0 * report.PassedExecutions / report.TotalExecutions, 1);
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\"/>");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");
        sb.AppendLine($"<title>Security Living Report — {WebUtility.HtmlEncode(meta.RunId)}</title>");
        sb.AppendLine(GetHtmlStyles());
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("""
<div class="top-banner">
  <strong>Browser mein open karein</strong> — Chrome ya Edge mein ye dashboard dikhega.
  Cursor / VS Code mein file khologe to HTML source code dikhega (normal hai).
</div>
""");

        var title = report.TotalScenarios > 1
            ? $"API Security Living Report — Consolidated Run ({report.TotalScenarios} scenarios)"
            : "API Security Living Documentation Report";

        sb.AppendLine("<header class=\"page-header\">");
        sb.AppendLine($"<h1>{WebUtility.HtmlEncode(title)}</h1>");
        sb.AppendLine($"<p class=\"meta\">Run ID: <strong>{WebUtility.HtmlEncode(meta.RunId)}</strong> | Mode: <strong>{WebUtility.HtmlEncode(meta.RunMode)}</strong> | Environment: <strong>{WebUtility.HtmlEncode(meta.Environment)}</strong></p>");
        sb.AppendLine($"<p class=\"meta\">Started: {localTime:yyyy-MM-dd HH:mm:ss} (local) | Report file: <strong>{WebUtility.HtmlEncode(reportFileName)}</strong> | <a href=\"index.html\" style=\"color:#93c5fd\">All runs</a></p>");
        sb.AppendLine("</header>");

        sb.AppendLine("<div class=\"cards\">");
        AppendStatCard(sb, "Scenarios Passed", $"{report.PassedScenarios}/{report.TotalScenarios}", report.FailedScenarios == 0 ? "pass" : "warn");
        AppendStatCard(sb, "Checks Passed", $"{report.PassedExecutions}/{report.TotalExecutions}", report.FailedExecutions == 0 ? "pass" : "warn");
        AppendStatCard(sb, "Success Rate", $"{successRate}%", successRate >= 90 ? "pass" : "fail");
        AppendStatCard(sb, "Failed Scenarios", report.FailedScenarios.ToString(), report.FailedScenarios == 0 ? "pass" : "fail");
        sb.AppendLine("</div>");

        sb.AppendLine("<nav class=\"tabs\" role=\"tablist\">");
        sb.AppendLine("<button type=\"button\" class=\"tab active\" data-panel=\"panel-dashboard\">Dashboard</button>");
        sb.AppendLine("<button type=\"button\" class=\"tab\" data-panel=\"panel-conclusion\">Run Conclusion</button>");
        sb.AppendLine("<button type=\"button\" class=\"tab\" data-panel=\"panel-scenarios\">Scenarios</button>");
        sb.AppendLine("<button type=\"button\" class=\"tab\" data-panel=\"panel-remediation\">Remediation</button>");
        sb.AppendLine("</nav>");

        // Panel 1 — Dashboard
        sb.AppendLine("<section id=\"panel-dashboard\" class=\"panel active\">");
        sb.AppendLine("<h2>Executive Summary</h2>");
        sb.AppendLine("<table class=\"meta-table\"><tbody>");
        AppendMetaRow(sb, "Project Name", meta.ProjectName);
        AppendMetaRow(sb, "Run Mode", meta.RunMode);
        AppendMetaRow(sb, "Test Filter", meta.TestFilter ?? "—");
        AppendMetaRow(sb, "Execution Date (UTC)", meta.ExecutedAtUtc.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
        if (meta.CompletedAtUtc.HasValue)
            AppendMetaRow(sb, "Completed (UTC)", meta.CompletedAtUtc.Value.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
        if (meta.Duration > TimeSpan.Zero)
            AppendMetaRow(sb, "Duration", meta.Duration.ToString(@"hh\:mm\:ss"));
        AppendMetaRow(sb, "Target Environment", meta.Environment);
        AppendMetaRow(sb, "API Base URL", meta.ApiBaseUrl);
        AppendMetaRow(sb, "Framework", meta.Framework);
        sb.AppendLine("</tbody></table>");

        sb.AppendLine("<h3>Summary Table</h3>");
        sb.AppendLine("<p class=\"table-hint\">Failed status par click karein — failed step aur reason yahi expand hoga.</p>");
        sb.AppendLine("<div class=\"table-wrap\"><table class=\"data-table\"><thead><tr>");
        sb.AppendLine("<th>Endpoint</th><th>Method</th><th>Vulnerability</th><th>Scenario</th><th>OWASP</th><th>Status</th><th>Severity</th>");
        sb.AppendLine("</tr></thead><tbody>");

        var rowIndex = 0;
        foreach (var row in report.DashboardRows)
        {
            var scenarioName = !string.IsNullOrWhiteSpace(row.ScenarioName)
                ? row.ScenarioName
                : row.Label.Contains(" :: ")
                    ? row.Label.Split(" :: ", 2)[0]
                    : row.Label;
            var statusClass = row.Status == SecurityTestStatus.Pass ? "pass" : "fail";
            var sevClass = SeverityCssClass(row.Severity);
            sb.AppendLine("<tr class=\"data-row\">");
            sb.AppendLine($"<td class=\"endpoint\"><code>{WebUtility.HtmlEncode(row.FullEndpointUrl)}</code></td>");
            sb.AppendLine($"<td>{WebUtility.HtmlEncode(row.HttpMethod)}</td>");
            sb.AppendLine($"<td>{WebUtility.HtmlEncode(row.VulnerabilityType)}</td>");
            sb.AppendLine($"<td>{WebUtility.HtmlEncode(scenarioName)}</td>");
            sb.AppendLine($"<td>{WebUtility.HtmlEncode(row.OwaspCategory)}</td>");

            if (row.Status == SecurityTestStatus.Fail)
            {
                var detailId = $"fail-detail-{rowIndex}";
                sb.AppendLine(
                    $"<td><button type=\"button\" class=\"status-pill fail fail-toggle\" aria-expanded=\"false\" aria-controls=\"{detailId}\" data-fail-toggle=\"{detailId}\">Fail</button></td>");
            }
            else
            {
                sb.AppendLine($"<td><span class=\"status-pill {statusClass}\">{row.Status}</span></td>");
            }

            sb.AppendLine($"<td><span class=\"sev-badge {sevClass}\">{SecurityOwaspMapper.FormatSeverity(row.Severity)}</span></td>");
            sb.AppendLine("</tr>");

            if (row.Status == SecurityTestStatus.Fail)
            {
                RenderDashboardFailDetailRow(sb, row, rowIndex);
            }

            rowIndex++;
        }

        sb.AppendLine("</tbody></table></div></section>");

        sb.AppendLine("<section id=\"panel-conclusion\" class=\"panel\">");
        RenderConclusionHtml(sb, report, successRate);
        sb.AppendLine("</section>");

        // Panel 2 — Scenarios
        sb.AppendLine("<section id=\"panel-scenarios\" class=\"panel\">");
        sb.AppendLine("<h2>Detailed Scenario Breakdown</h2>");
        foreach (var scenario in report.Scenarios)
            RenderScenarioHtmlCard(sb, scenario);
        sb.AppendLine("</section>");

        // Panel 3 — Remediation (collapsible — C# snippets hidden until expanded)
        sb.AppendLine("<section id=\"panel-remediation\" class=\"panel\">");
        sb.AppendLine("<h2>Actionable Remediation (.NET C#)</h2>");
        RenderRemediationHtml(sb, report);
        sb.AppendLine("</section>");

        sb.AppendLine(GetHtmlTabScript());
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string GetHtmlStyles() =>
        """
<style>
  :root { --bg:#f0f4f8; --card:#fff; --text:#1e293b; --muted:#64748b; --pass:#059669; --fail:#dc2626; --warn:#d97706; --border:#e2e8f0; }
  * { box-sizing: border-box; }
  body { font-family: "Segoe UI", Arial, sans-serif; margin: 0; background: var(--bg); color: var(--text); line-height: 1.55; }
  .top-banner { background: #1e40af; color: #fff; padding: 10px 24px; font-size: 14px; text-align: center; }
  .page-header { background: linear-gradient(135deg,#0f172a,#1e3a5f); color: #fff; padding: 28px 32px; }
  .page-header h1 { margin: 0 0 8px; font-size: 1.75rem; }
  .page-header .meta { margin: 0; opacity: .9; font-size: 14px; }
  .cards { display: grid; grid-template-columns: repeat(auto-fit,minmax(160px,1fr)); gap: 14px; padding: 20px 32px; max-width: 1400px; margin: 0 auto; }
  .stat-card { background: var(--card); border-radius: 12px; padding: 18px; box-shadow: 0 2px 10px rgba(0,0,0,.07); border-left: 4px solid var(--border); }
  .stat-card.pass { border-left-color: var(--pass); }
  .stat-card.fail { border-left-color: var(--fail); }
  .stat-card.warn { border-left-color: var(--warn); }
  .stat-card .label { font-size: 13px; color: var(--muted); margin-bottom: 4px; }
  .stat-card .value { font-size: 1.6rem; font-weight: 700; }
  .tabs { display: flex; gap: 8px; padding: 0 32px; max-width: 1400px; margin: 0 auto; flex-wrap: wrap; }
  .tab { border: none; background: #cbd5e1; color: #334155; padding: 10px 20px; border-radius: 8px 8px 0 0; cursor: pointer; font-size: 14px; font-weight: 600; }
  .tab.active { background: var(--card); color: #0f172a; box-shadow: 0 -2px 6px rgba(0,0,0,.06); }
  .panel { display: none; max-width: 1400px; margin: 0 auto 32px; padding: 24px 32px; background: var(--card); border-radius: 0 12px 12px 12px; box-shadow: 0 4px 16px rgba(0,0,0,.08); }
  .panel.active { display: block; }
  h2 { margin-top: 0; color: #0f172a; font-size: 1.35rem; }
  h3 { color: #1e3a5f; font-size: 1.1rem; margin-top: 0; }
  .meta-table, .data-table { width: 100%; border-collapse: collapse; font-size: 14px; }
  .meta-table td { padding: 8px 12px; border-bottom: 1px solid var(--border); }
  .meta-table td:first-child { font-weight: 600; width: 200px; color: var(--muted); }
  .table-wrap { overflow-x: auto; margin-top: 12px; }
  .data-table th { background: #0f172a; color: #fff; padding: 10px 12px; text-align: left; white-space: nowrap; }
  .data-table td { padding: 10px 12px; border-bottom: 1px solid var(--border); vertical-align: top; }
  .data-table tr:hover td { background: #f8fafc; }
  .data-table .endpoint code { font-size: 12px; word-break: break-all; }
  .status-pill { display: inline-block; padding: 3px 10px; border-radius: 999px; font-size: 12px; font-weight: 700; }
  .status-pill.pass { background: #d1fae5; color: #065f46; }
  .status-pill.fail { background: #fee2e2; color: #991b1b; }
  .sev-badge { display: inline-block; padding: 3px 8px; border-radius: 6px; font-size: 11px; font-weight: 700; }
  .sev-badge.critical { background: #fecaca; color: #7f1d1d; }
  .sev-badge.high { background: #fed7aa; color: #9a3412; }
  .sev-badge.medium { background: #fef08a; color: #854d0e; }
  .sev-badge.low { background: #dcfce7; color: #166534; }
  .scenario-card { border: 1px solid var(--border); border-radius: 10px; padding: 16px 20px; margin-bottom: 16px; background: #fafbfc; }
  .scenario-card.fail { border-left: 4px solid var(--fail); }
  .scenario-card.pass { border-left: 4px solid var(--pass); }
  .scenario-meta { list-style: none; padding: 0; margin: 0 0 12px; font-size: 14px; }
  .scenario-meta li { margin: 4px 0; }
  .step-list { margin: 0; padding-left: 20px; font-size: 14px; }
  .step-list li { margin-bottom: 8px; }
  .step-ok::marker { content: "✅ "; }
  .step-fail::marker { content: "❌ "; }
  .result-box { background: #fff; border: 1px solid var(--border); border-radius: 8px; padding: 12px; margin-top: 10px; font-size: 14px; }
  details.remediation-block { border: 1px solid var(--border); border-radius: 10px; margin-bottom: 14px; background: #fff; }
  details.remediation-block summary { cursor: pointer; padding: 14px 18px; font-weight: 600; background: #f1f5f9; border-radius: 10px; }
  details.remediation-block[open] summary { border-radius: 10px 10px 0 0; border-bottom: 1px solid var(--border); }
  details.remediation-block .inner { padding: 16px 18px; }
  pre.code-block { background: #0f172a; color: #e2e8f0; padding: 14px; border-radius: 8px; overflow: auto; font-size: 12px; line-height: 1.45; margin: 8px 0 16px; white-space: pre-wrap; }
  .code-label { font-size: 13px; font-weight: 700; margin: 12px 0 6px; color: var(--muted); }
  .empty-state { color: var(--muted); font-style: italic; padding: 16px; }
  .fail-reason { color: var(--fail); font-size: 13px; margin-top: 4px; display: block; }
  .conclusion-verdict { font-size: 1.2rem; font-weight: 700; margin: 12px 0; }
  .conclusion-footer { margin-top: 24px; color: var(--muted); font-size: 14px; }
  .table-hint { font-size: 13px; color: var(--muted); margin: 0 0 10px; }
  .status-pill.fail-toggle { cursor: pointer; border: none; font-family: inherit; }
  .status-pill.fail-toggle:hover { filter: brightness(0.95); }
  .status-pill.fail-toggle[aria-expanded="true"] { outline: 2px solid #991b1b; }
  .fail-detail-row td { background: #fef2f2; padding: 12px 16px; }
  .fail-detail-box { font-size: 13px; line-height: 1.5; margin: 0; }
  .fail-detail-box dt { font-weight: 600; color: var(--muted); margin-top: 6px; }
  .fail-detail-box dt:first-child { margin-top: 0; }
  .fail-detail-box dd { margin: 2px 0 0; }
</style>
""";

    public static string RenderRunIndexHtml(IReadOnlyList<SecurityRunManifestEntry> runs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\"/>");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");
        sb.AppendLine("<title>Security Report Run History</title>");
        sb.AppendLine(GetHtmlStyles());
        sb.AppendLine("<style>.run-link{color:#2563eb;font-weight:600;text-decoration:none}.run-link:hover{text-decoration:underline}</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<header class=\"page-header\">");
        sb.AppendLine("<h1>Security Report Run History</h1>");
        sb.AppendLine("<p class=\"meta\">Har test run ki alag timestamped HTML file — neeche list se kholen.</p>");
        sb.AppendLine("</header>");
        sb.AppendLine("<section class=\"panel active\" style=\"display:block;margin:24px auto;border-radius:12px\">");
        sb.AppendLine("<h2>All Runs</h2>");

        if (runs.Count == 0)
        {
            sb.AppendLine("<p class=\"empty-state\">No runs recorded yet. Execute .\\scripts\\run-security-report.ps1</p>");
        }
        else
        {
            sb.AppendLine("<div class=\"table-wrap\"><table class=\"data-table\"><thead><tr>");
            sb.AppendLine("<th>Run ID</th><th>Mode</th><th>Executed (Local)</th><th>Scenarios</th><th>Success</th><th>Report</th>");
            sb.AppendLine("</tr></thead><tbody>");

            foreach (var run in runs)
            {
                var localTime = run.ExecutedAtUtc.ToLocalTime();
                var successRate = run.TotalScenarios == 0
                    ? 100.0
                    : Math.Round(100.0 * run.PassedScenarios / run.TotalScenarios, 1);
                var statusClass = run.FailedScenarios == 0 ? "pass" : "fail";
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td><code>{WebUtility.HtmlEncode(run.RunId)}</code></td>");
                sb.AppendLine($"<td>{WebUtility.HtmlEncode(run.RunMode)}</td>");
                sb.AppendLine($"<td>{localTime:yyyy-MM-dd HH:mm:ss}</td>");
                sb.AppendLine($"<td>{run.PassedScenarios}/{run.TotalScenarios} passed</td>");
                sb.AppendLine($"<td><span class=\"status-pill {statusClass}\">{successRate}%</span></td>");
                sb.AppendLine($"<td><a class=\"run-link\" href=\"{WebUtility.HtmlEncode(run.HtmlFile)}\">Open HTML</a></td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table></div>");
        }

        sb.AppendLine("</section>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string GetHtmlTabScript() =>
        """
<script>
(function(){
  var tabs = document.querySelectorAll('.tab');
  var panels = document.querySelectorAll('.panel');
  tabs.forEach(function(tab){
    tab.addEventListener('click', function(){
      var id = tab.getAttribute('data-panel');
      tabs.forEach(function(t){ t.classList.remove('active'); });
      panels.forEach(function(p){ p.classList.remove('active'); });
      tab.classList.add('active');
      var panel = document.getElementById(id);
      if (panel) panel.classList.add('active');
    });
  });

  document.querySelectorAll('[data-fail-toggle]').forEach(function(btn){
    btn.addEventListener('click', function(){
      var id = btn.getAttribute('data-fail-toggle');
      var row = document.getElementById(id);
      if (!row) return;
      var open = row.hidden;
      row.hidden = !open;
      btn.setAttribute('aria-expanded', open ? 'true' : 'false');
    });
  });
})();
</script>
""";

    private static void RenderDashboardFailDetailRow(StringBuilder sb, SecurityExecutionResult row, int rowIndex)
    {
        var detailId = $"fail-detail-{rowIndex}";
        var failedStep = row.FailedStepText ?? "—";
        var reason = row.FailureSummary ?? row.ResultReason ?? row.ErrorMessage ?? "—";
        var expectedActual = row.ExpectedStatus.HasValue || row.ActualStatus.HasValue
            ? $"{row.ExpectedStatus}/{row.ActualStatus}"
            : "—";

        sb.AppendLine($"<tr id=\"{detailId}\" class=\"fail-detail-row\" hidden>");
        sb.AppendLine("<td colspan=\"7\">");
        sb.AppendLine("<dl class=\"fail-detail-box\">");
        sb.AppendLine($"<dt>Failed Step</dt><dd>{WebUtility.HtmlEncode(failedStep)}</dd>");
        sb.AppendLine($"<dt>Reason</dt><dd>{WebUtility.HtmlEncode(reason)}</dd>");
        sb.AppendLine($"<dt>Expected / Actual</dt><dd><code>{WebUtility.HtmlEncode(expectedActual)}</code></dd>");
        if (!string.IsNullOrWhiteSpace(row.ErrorMessage) && row.ErrorMessage != reason)
            sb.AppendLine($"<dt>Detail</dt><dd>{WebUtility.HtmlEncode(row.ErrorMessage)}</dd>");
        sb.AppendLine("</dl></td></tr>");
    }

    private static void RenderConclusionHtml(StringBuilder sb, SecurityLivingReport report, double successRate)
    {
        var meta = report.Metadata;
        var scenarioSuccessRate = report.TotalScenarios == 0
            ? 100.0
            : Math.Round(100.0 * report.PassedScenarios / report.TotalScenarios, 1);
        var concludedLocal = (meta.CompletedAtUtc ?? meta.ExecutedAtUtc).ToLocalTime();
        var verdictClass = report.FailedScenarios == 0 ? "pass" : "fail";

        sb.AppendLine("<h2>Run Conclusion</h2>");
        sb.AppendLine("<table class=\"meta-table\"><tbody>");
        AppendMetaRow(sb, "Run ID", meta.RunId);
        AppendMetaRow(sb, "Run Mode", meta.RunMode);
        AppendMetaRow(sb, "Test Filter", meta.TestFilter ?? "—");
        AppendMetaRow(sb, "Scenarios", $"{report.PassedScenarios}/{report.TotalScenarios} passed");
        AppendMetaRow(sb, "Checks", $"{report.PassedExecutions}/{report.TotalExecutions} passed");
        if (meta.Duration > TimeSpan.Zero)
            AppendMetaRow(sb, "Duration", meta.Duration.ToString(@"hh\:mm\:ss"));
        sb.AppendLine("</tbody></table>");

        sb.AppendLine($"<p class=\"conclusion-verdict\">Final verdict: <span class=\"status-pill {verdictClass}\">{report.PassedScenarios}/{report.TotalScenarios} scenarios passed ({scenarioSuccessRate}%)</span> | Checks success: {successRate}%</p>");

        var failedScenarios = report.Scenarios.Where(s => s.OverallStatus == SecurityTestStatus.Fail).ToList();
        if (failedScenarios.Count == 0)
        {
            sb.AppendLine("<p class=\"empty-state\">All scenarios passed — no failures in this run.</p>");
        }
        else
        {
            sb.AppendLine("<h3>Failed Scenarios</h3>");
            sb.AppendLine("<div class=\"table-wrap\"><table class=\"data-table\"><thead><tr>");
            sb.AppendLine("<th>Scenario</th><th>Failed Step</th><th>Reason</th><th>Expected/Actual</th>");
            sb.AppendLine("</tr></thead><tbody>");

            foreach (var scenario in failedScenarios)
            {
                var failedExec = scenario.Executions.FirstOrDefault(e => e.Status == SecurityTestStatus.Fail);
                var expectedActual = failedExec != null
                    ? $"{failedExec.ExpectedStatus}/{failedExec.ActualStatus}"
                    : "—";
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{WebUtility.HtmlEncode(scenario.ScenarioName)}</td>");
                sb.AppendLine($"<td><code>{WebUtility.HtmlEncode(scenario.FailedStepText ?? "—")}</code></td>");
                sb.AppendLine($"<td>{WebUtility.HtmlEncode(scenario.FailureSummary ?? scenario.ScenarioError ?? "—")}</td>");
                sb.AppendLine($"<td><code>{WebUtility.HtmlEncode(expectedActual)}</code></td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table></div>");
        }

        sb.AppendLine($"<p class=\"conclusion-footer\">Run concluded at {concludedLocal:yyyy-MM-dd HH:mm:ss} (local).</p>");
    }

    private static void AppendStatCard(StringBuilder sb, string label, string value, string tone)
    {
        sb.AppendLine($"<div class=\"stat-card {tone}\"><div class=\"label\">{WebUtility.HtmlEncode(label)}</div><div class=\"value\">{WebUtility.HtmlEncode(value)}</div></div>");
    }

    private static void RenderRemediationHtml(StringBuilder sb, SecurityLivingReport report)
    {
        var failed = report.Scenarios
            .SelectMany(s => s.Executions)
            .Where(e => e.Status == SecurityTestStatus.Fail)
            .ToList();

        if (failed.Count == 0)
        {
            sb.AppendLine("<p class=\"empty-state\">No failed scenarios — no remediation required for this run.</p>");
            return;
        }

        sb.AppendLine("<p>Failed checks ke liye neeche C# fixes expand karke dekho (click to open).</p>");

        foreach (var remediation in SecurityRemediationCatalog.GetForFailedExecutions(failed))
        {
            sb.AppendLine("<details class=\"remediation-block\">");
            sb.AppendLine($"<summary>{WebUtility.HtmlEncode(remediation.Title)}</summary>");
            sb.AppendLine("<div class=\"inner\">");
            sb.AppendLine("<div class=\"code-label\">Vulnerable C# (ASP.NET Core)</div>");
            sb.AppendLine($"<pre class=\"code-block\">{WebUtility.HtmlEncode(remediation.VulnerableCode.TrimEnd())}</pre>");
            sb.AppendLine("<div class=\"code-label\">Secured C# (ASP.NET Core)</div>");
            sb.AppendLine($"<pre class=\"code-block\">{WebUtility.HtmlEncode(remediation.SecuredCode.TrimEnd())}</pre>");
            sb.AppendLine("</div></details>");
        }
    }

    private static string SeverityCssClass(SecuritySeverity severity) =>
        severity == SecuritySeverity.Informational ? "low" : severity.ToString().ToLowerInvariant();

    private static void RenderScenarioMarkdown(StringBuilder sb, SecurityScenarioReport scenario)
    {
        sb.AppendLine($"### {scenario.ScenarioName}");
        sb.AppendLine();
        sb.AppendLine($"- **Feature:** {scenario.FeatureName}");
        sb.AppendLine($"- **Suite:** {scenario.Suite}");
        sb.AppendLine($"- **Overall Status:** **{scenario.OverallStatus}**");

        if (scenario.Executions.Count > 0)
        {
            var first = scenario.Executions[0];
            sb.AppendLine($"- **Endpoint:** {first.HttpMethod} `{first.FullEndpointUrl}`");
            sb.AppendLine($"- **OWASP:** {first.OwaspCategory} | **Severity:** {SecurityOwaspMapper.FormatSeverity(first.Severity)}");
        }

        sb.AppendLine();
        sb.AppendLine("#### Gherkin Steps & Step-by-step");
        sb.AppendLine();

        foreach (var step in scenario.Steps)
        {
            var icon = step.Status == SecurityTestStatus.Pass ? "PASS" : "FAIL";
            sb.AppendLine($"- **{step.StepType}** {step.StepText} — *{step.Narrative}* **[{icon}]**");
        }

        sb.AppendLine();
        sb.AppendLine("#### Result & Reason Breakdown");
        sb.AppendLine();

        foreach (var exec in scenario.Executions)
        {
            sb.AppendLine($"- **{exec.VulnerabilityType}** — **{exec.Status}**");
            sb.AppendLine($"  - Expected: `{exec.ExpectedStatus}` | Actual: `{exec.ActualStatus}`");
            sb.AppendLine($"  - Reason: {exec.ResultReason}");
            if (!string.IsNullOrWhiteSpace(exec.ErrorMessage))
                sb.AppendLine($"  - Detail: {exec.ErrorMessage}");
        }

        if (!string.IsNullOrWhiteSpace(scenario.ScenarioError))
            sb.AppendLine($"- **Scenario Error:** {scenario.ScenarioError}");

        sb.AppendLine();
    }

    private static void RenderScenarioHtmlCard(StringBuilder sb, SecurityScenarioReport scenario)
    {
        var cardClass = scenario.OverallStatus == SecurityTestStatus.Pass ? "pass" : "fail";
        sb.AppendLine($"<article class=\"scenario-card {cardClass}\">");
        sb.AppendLine($"<h3>{WebUtility.HtmlEncode(scenario.ScenarioName)}</h3>");
        sb.AppendLine("<ul class=\"scenario-meta\">");
        sb.AppendLine($"<li><strong>Feature:</strong> {WebUtility.HtmlEncode(scenario.FeatureName)} | <strong>Suite:</strong> {WebUtility.HtmlEncode(scenario.Suite)}</li>");
        sb.AppendLine($"<li><strong>Status:</strong> <span class=\"status-pill {cardClass}\">{scenario.OverallStatus}</span></li>");

        if (scenario.Executions.Count > 0)
        {
            var first = scenario.Executions[0];
            sb.AppendLine($"<li><strong>Endpoint:</strong> {WebUtility.HtmlEncode(first.HttpMethod)} <code>{WebUtility.HtmlEncode(first.FullEndpointUrl)}</code></li>");
            sb.AppendLine($"<li><strong>OWASP:</strong> {WebUtility.HtmlEncode(first.OwaspCategory)} | <strong>Severity:</strong> {SecurityOwaspMapper.FormatSeverity(first.Severity)}</li>");
        }

        sb.AppendLine("</ul>");
        sb.AppendLine("<h4>Step-by-step</h4><ol class=\"step-list\">");

        foreach (var step in scenario.Steps)
        {
            var css = step.Status == SecurityTestStatus.Pass ? "step-ok" : "step-fail";
            sb.AppendLine($"<li class=\"{css}\"><strong>{WebUtility.HtmlEncode(step.StepType)}</strong> {WebUtility.HtmlEncode(step.StepText)}<br/><em>{WebUtility.HtmlEncode(step.Narrative)}</em>");
            if (!string.IsNullOrWhiteSpace(step.FailureReason))
                sb.AppendLine($"<span class=\"fail-reason\">Reason: {WebUtility.HtmlEncode(step.FailureReason)}</span>");
            sb.AppendLine("</li>");
        }

        sb.AppendLine("</ol>");

        foreach (var exec in scenario.Executions)
        {
            var execClass = exec.Status == SecurityTestStatus.Pass ? "pass" : "fail";
            sb.AppendLine("<div class=\"result-box\">");
            sb.AppendLine($"<strong>{WebUtility.HtmlEncode(exec.VulnerabilityType)}</strong> — <span class=\"status-pill {execClass}\">{exec.Status}</span><br/>");
            sb.AppendLine($"Expected: <code>{exec.ExpectedStatus}</code> | Actual: <code>{exec.ActualStatus}</code><br/>");
            sb.AppendLine(WebUtility.HtmlEncode(exec.ResultReason ?? string.Empty));
            if (!string.IsNullOrWhiteSpace(exec.ErrorMessage))
                sb.AppendLine($"<br/><strong>Detail:</strong> {WebUtility.HtmlEncode(exec.ErrorMessage)}");
            sb.AppendLine("</div>");
        }

        if (!string.IsNullOrWhiteSpace(scenario.ScenarioError))
        {
            sb.AppendLine("<div class=\"result-box\">");
            sb.AppendLine($"<strong>Scenario Error</strong><br/>{WebUtility.HtmlEncode(scenario.ScenarioError)}");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</article>");
    }

    private static void AppendMetaRow(StringBuilder sb, string label, string value)
    {
        sb.AppendLine($"<tr><td>{WebUtility.HtmlEncode(label)}</td><td>{WebUtility.HtmlEncode(value)}</td></tr>");
    }

    private static string EscapePipe(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal);
}
