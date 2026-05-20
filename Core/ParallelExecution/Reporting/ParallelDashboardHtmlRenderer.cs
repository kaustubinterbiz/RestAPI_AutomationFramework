using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnterpriseApiAutomationFramework.Core.ParallelExecution.Models;

namespace EnterpriseApiAutomationFramework.Core.ParallelExecution.Reporting;

/// <summary>
/// Renders the parallel consolidated HTML dashboard with Chart.js (parallel runs only).
/// </summary>
public static class ParallelDashboardHtmlRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Render(ConsolidatedRunReport report, ParallelDashboardChartData chartData)
    {
        var chartJson = JsonSerializer.Serialize(chartData, JsonOptions);
        var rows = BuildModuleTableRows(report.Modules);
        var parallelInfo = report.ParallelStatistics != null
            ? $@"<p class=""meta"">
              <strong>Parallel:</strong> Max workers {report.ParallelStatistics.MaxConcurrentWorkers} |
              Wall-clock {report.ParallelStatistics.WallClockDurationMs:F0} ms |
              Time saved ~{report.ParallelStatistics.TimeSavedMs:F0} ms |
              Efficiency {report.ParallelStatistics.ParallelismEfficiencyPercent:F1}% |
              Concurrency: {(report.ParallelStatistics.AchievedConcurrency ? "Yes" : "No")}
            </p>"
            : string.Empty;

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""utf-8""/>
  <meta name=""viewport"" content=""width=device-width, initial-scale=1""/>
  <title>Parallel Execution Dashboard - {WebUtility.HtmlEncode(report.RunId)}</title>
  <script src=""https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js""></script>
  <style>
    body {{ font-family: Segoe UI, Arial, sans-serif; margin: 24px; background: #f5f7fb; color: #1f2937; }}
    h1, h2 {{ margin-bottom: 8px; }}
    .meta {{ color: #4b5563; }}
    .cards {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(170px, 1fr)); gap: 12px; margin: 20px 0; }}
    .card {{ background: #fff; border-radius: 10px; padding: 16px; box-shadow: 0 2px 8px rgba(0,0,0,.08); }}
    .card .value {{ font-size: 1.5rem; font-weight: 700; }}
    .charts-grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 16px; margin: 20px 0; }}
    .chart-card {{ background: #fff; border-radius: 10px; padding: 16px; box-shadow: 0 2px 8px rgba(0,0,0,.08); }}
    .chart-card h3 {{ margin: 0 0 12px; font-size: 1rem; }}
    .chart-wrap {{ position: relative; height: 220px; }}
    .timeline-wrap {{ position: relative; height: 420px; background: #fff; border-radius: 10px; padding: 16px; box-shadow: 0 2px 8px rgba(0,0,0,.08); margin: 20px 0; }}
    table {{ width: 100%; border-collapse: collapse; background: #fff; border-radius: 10px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,.08); }}
    th, td {{ padding: 10px 12px; border-bottom: 1px solid #e5e7eb; text-align: left; vertical-align: top; }}
    th {{ background: #111827; color: #fff; }}
    .badge {{ padding: 3px 8px; border-radius: 999px; font-size: .8rem; }}
    .pass {{ background: #d1fae5; color: #065f46; }}
    .fail {{ background: #fee2e2; color: #991b1b; }}
    .downloads a {{ margin-right: 12px; }}
    .chart-empty {{ color: #6b7280; font-size: .9rem; padding: 24px; text-align: center; }}
  </style>
</head>
<body>
  <h1>Parallel Execution Consolidated Dashboard</h1>
  <p class=""meta"">
    Run ID: <strong>{WebUtility.HtmlEncode(report.RunId)}</strong> |
    Started: {report.StartTimeUtc:O} |
    Ended: {report.EndTimeUtc:O} |
    Duration: {report.TotalDuration}
  </p>
  {parallelInfo}
  <div class=""downloads"">
    <a href=""consolidated-report.json"" download>Download JSON</a>
    <a href=""chart-data.json"" download>Download Chart Data</a>
    <a href=""consolidated-summary.txt"" download>Download Table Summary</a>
  </div>
  <div class=""cards"">
    <div class=""card""><div>Modules Passed</div><div class=""value"">{report.PassedModules}/{report.TotalModules}</div></div>
    <div class=""card""><div>Scenarios Passed</div><div class=""value"">{report.PassedScenarios}/{report.TotalScenarios}</div></div>
    <div class=""card""><div>Success Rate</div><div class=""value"">{report.SuccessRatePercent:F1}%</div></div>
    <div class=""card""><div>Avg Module Time</div><div class=""value"">{report.AverageModuleDurationMs:F0} ms</div></div>
    <div class=""card""><div>Max Module Time</div><div class=""value"">{report.MaxModuleDurationMs:F0} ms</div></div>
    <div class=""card""><div>Min Module Time</div><div class=""value"">{report.MinModuleDurationMs:F0} ms</div></div>
  </div>

  <h2>Analytics</h2>
  <div class=""charts-grid"">
    <div class=""chart-card""><h3>Features (Passed vs Failed)</h3><div class=""chart-wrap""><canvas id=""chart-features""></canvas></div></div>
    <div class=""chart-card""><h3>Scenario Distribution</h3><div class=""chart-wrap""><canvas id=""chart-scenarios""></canvas></div></div>
    <div class=""chart-card""><h3>Step Status Summary</h3><div class=""chart-wrap""><canvas id=""chart-steps""></canvas></div></div>
    <div class=""chart-card""><h3>Execution Result Distribution</h3><div class=""chart-wrap""><canvas id=""chart-results""></canvas></div></div>
  </div>

  <h2>Parallel Execution Timeline</h2>
  <p class=""meta"">Gantt-style view of concurrent worker execution (offset from run start).</p>
  <div class=""timeline-wrap""><canvas id=""chart-timeline""></canvas></div>

  <h2>Module Results</h2>
  <table>
    <thead>
      <tr>
        <th>Module</th><th>Status</th><th>Worker PID</th><th>Start</th><th>End</th><th>Duration</th>
        <th>Scenarios</th><th>Exit</th><th>Error</th><th>Artifacts</th>
      </tr>
    </thead>
    <tbody>{rows}</tbody>
  </table>

  <script id=""parallel-chart-data"" type=""application/json"">{chartJson}</script>
  <script>
    (function () {{
      function ready(fn) {{
        if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', fn);
        else fn();
      }}

      function sum(values) {{
        return (values || []).reduce((a, b) => a + (Number(b) || 0), 0);
      }}

      function createPie(canvasId, dataset, title) {{
        const el = document.getElementById(canvasId);
        if (!el || !dataset) return;
        const values = dataset.values || [];
        if (sum(values) <= 0) {{
          el.parentElement.innerHTML = '<div class=""chart-empty"">No data for ' + title + '</div>';
          return;
        }}
        new Chart(el, {{
          type: 'pie',
          data: {{
            labels: dataset.labels,
            datasets: [{{
              data: values,
              backgroundColor: dataset.colors
            }}]
          }},
          options: {{
            responsive: true,
            maintainAspectRatio: false,
            plugins: {{
              legend: {{ position: 'bottom' }},
              tooltip: {{
                callbacks: {{
                  label: function(ctx) {{
                    const total = sum(ctx.dataset.data);
                    const pct = total ? ((ctx.raw / total) * 100).toFixed(1) : 0;
                    return ctx.label + ': ' + ctx.raw + ' (' + pct + '%)';
                  }}
                }}
              }}
            }}
          }}
        }});
      }}

      function createTimeline(canvasId, timeline, totalMs) {{
        const el = document.getElementById(canvasId);
        if (!el || !timeline || timeline.length === 0) {{
          if (el) el.parentElement.innerHTML = '<div class=""chart-empty"">No timeline data</div>';
          return;
        }}

        const labels = timeline.map(t => t.label);
        const colors = timeline.map(t => t.status === 'Success' ? '#22c55e' : (t.status === 'Skipped' ? '#94a3b8' : '#ef4444'));

        new Chart(el, {{
          type: 'bar',
          data: {{
            labels: labels,
            datasets: [{{
              label: 'Execution window (ms from run start)',
              data: timeline.map(t => [t.startOffsetMs, t.endOffsetMs]),
              backgroundColor: colors,
              borderColor: colors,
              borderWidth: 1,
              barPercentage: 0.7
            }}]
          }},
          options: {{
            indexAxis: 'y',
            responsive: true,
            maintainAspectRatio: false,
            parsing: {{ xAxisKey: 'x', yAxisKey: 'y' }},
            plugins: {{
              legend: {{ display: false }},
              tooltip: {{
                callbacks: {{
                  label: function(ctx) {{
                    const t = timeline[ctx.dataIndex];
                    return [
                      'Worker: ' + (t.workerOsProcessId || 'n/a'),
                      'Start: ' + t.startOffsetMs.toFixed(0) + ' ms',
                      'End: ' + t.endOffsetMs.toFixed(0) + ' ms',
                      'Duration: ' + t.durationMs.toFixed(0) + ' ms'
                    ];
                  }}
                }}
              }}
            }},
            scales: {{
              x: {{
                min: 0,
                max: Math.max(totalMs, 1),
                title: {{ display: true, text: 'Milliseconds from run start' }}
              }},
              y: {{
                ticks: {{ autoSkip: false }}
              }}
            }}
          }}
        }});
      }}

      ready(function () {{
        let data;
        try {{
          const node = document.getElementById('parallel-chart-data');
          data = JSON.parse(node.textContent || '{{}}');
        }} catch (e) {{
          console.error('Failed to parse parallel chart data', e);
          return;
        }}

        if (typeof Chart === 'undefined') {{
          console.error('Chart.js failed to load');
          return;
        }}

        createPie('chart-features', data.features, 'Features');
        createPie('chart-scenarios', data.scenarios, 'Scenarios');
        createPie('chart-steps', data.steps, 'Steps');
        createPie('chart-results', data.executionResults, 'Results');
        createTimeline('chart-timeline', data.timeline, data.totalRunDurationMs);
      }});
    }})();
  </script>
</body>
</html>";
    }

    private static string BuildModuleTableRows(IReadOnlyList<FeatureExecutionResult> modules)
    {
        var sb = new StringBuilder();
        foreach (var module in modules)
        {
            var statusClass = module.Status == ExecutionStatus.Success ? "pass" : "fail";
            var scenarioPass = module.Scenarios.Count(s => s.Status == ExecutionStatus.Success);
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{WebUtility.HtmlEncode(module.ModuleName)}</td>");
            sb.AppendLine($"<td><span class=\"badge {statusClass}\">{module.Status}</span></td>");
            sb.AppendLine($"<td>{module.WorkerOsProcessId?.ToString() ?? "-"}</td>");
            sb.AppendLine($"<td>{module.StartTimeUtc:HH:mm:ss.fff} UTC</td>");
            sb.AppendLine($"<td>{module.EndTimeUtc:HH:mm:ss.fff} UTC</td>");
            sb.AppendLine($"<td>{module.Duration.TotalSeconds:F2}s</td>");
            sb.AppendLine($"<td>{scenarioPass}/{module.Scenarios.Count}</td>");
            sb.AppendLine($"<td>{module.ExitCode}</td>");
            sb.AppendLine($"<td>{WebUtility.HtmlEncode(module.ErrorSummary ?? "-")}</td>");
            sb.AppendLine(
                $"<td><a href=\"file:///{WebUtility.HtmlEncode(module.ExtentReportPath ?? "#")}\">Extent</a> | " +
                $"<a href=\"file:///{WebUtility.HtmlEncode(module.LogFilePath ?? "#")}\">Log</a></td>");
            sb.AppendLine("</tr>");
        }

        return sb.ToString();
    }
}
