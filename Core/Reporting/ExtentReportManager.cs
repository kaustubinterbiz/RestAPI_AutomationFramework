using System.Text;
using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using AventStack.ExtentReports.Reporter.Config;
using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Reporting;

public static class ExtentReportManager
{
    private static ExtentReports? _extent;
    private static string? _reportFilePath;

    public static ExtentReports Instance =>
        _extent ?? throw new InvalidOperationException("ExtentReports has not been initialized. Call Initialize() first.");

    public static string? ReportFilePath => _reportFilePath;

    public static void Initialize()
    {
        if (_extent != null)
            return;

        ReportExecutionContext.Configure();

        var reportDir = ConfigReader.GetValue("ReportPath");
        if (string.IsNullOrWhiteSpace(reportDir))
            reportDir = "Reports/Html";

        var fullReportDir = Path.IsPathRooted(reportDir)
            ? reportDir
            : Path.GetFullPath(Path.Combine(ResolveProjectRoot(), reportDir));
        Directory.CreateDirectory(fullReportDir);
        _reportFilePath = Path.Combine(
            fullReportDir,
            $"ApiTestReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        var spark = new ExtentSparkReporter(_reportFilePath);
        spark.Config.DocumentTitle = "API Automation Report";
        spark.Config.ReportName = "RestSharp API Automation Framework";
        spark.Config.Theme = ResolveTheme();
        spark.Config.TimeStampFormat = "dd-MM-yyyy HH:mm:ss";
        spark.Config.TimelineEnabled = true;

        _extent = new ExtentReports();
        _extent.AttachReporter(spark);
        _extent.AddSystemInfo("Environment", ConfigReader.GetValue("Environment"));
        _extent.AddSystemInfo("Base URL", ConfigReader.GetValue("BaseUrl"));
        _extent.AddSystemInfo(
            "Expected API Response Time (ms)",
            ReportExecutionContext.ExpectedResponseTimeMs.ToString());
    }

    public static void Flush()
    {
        _extent?.Flush();
        if (!string.IsNullOrWhiteSpace(_reportFilePath))
            InjectReportCustomizations(_reportFilePath);
        if (!string.IsNullOrWhiteSpace(_reportFilePath))
            Console.WriteLine($"Extent report generated: {Path.GetFullPath(_reportFilePath)}");
    }

    /// <summary>
    /// Walks up from the test output directory to find the folder containing the .csproj.
    /// </summary>
    private static string ResolveProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (dir.GetFiles("*.csproj").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static Theme ResolveTheme()
    {
        var theme = ConfigReader.GetValue("ExtentReportTheme");
        return string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase)
            ? Theme.Dark
            : Theme.Standard;
    }

    /// <summary>
    /// Injects custom CSS/JS and default theme flag into the generated HTML report.
    /// </summary>
    private static void InjectReportCustomizations(string reportPath)
    {
        var html = File.ReadAllText(reportPath);
        var templatesDir = Path.Combine(AppContext.BaseDirectory, "Reporting", "Templates");
        var customCssPath = Path.Combine(templatesDir, "extent-report-custom.css");
        var customJsPath = Path.Combine(templatesDir, "extent-report-custom.js");

        if (File.Exists(customCssPath))
        {
            var css = File.ReadAllText(customCssPath);
            html = html.Replace("<style></style>", $"<style>{css}</style>");
        }

        var defaultDark = ResolveTheme() == Theme.Dark;
        var scriptBlock = new StringBuilder();
        scriptBlock.AppendLine($"<script>window.extentReportDefaultDark = {(defaultDark ? "true" : "false")};</script>");
        if (File.Exists(customJsPath))
            scriptBlock.AppendLine($"<script>{File.ReadAllText(customJsPath)}</script>");

        const string marker = "<script type='text/javascript'></script>";
        if (html.Contains(marker))
            html = html.Replace(marker, scriptBlock.ToString());
        else
            html = html.Replace("</body>", $"{scriptBlock}</body>");

        File.WriteAllText(reportPath, html);
    }
}
