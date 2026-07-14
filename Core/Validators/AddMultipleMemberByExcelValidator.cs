using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Helpers;
using FluentAssertions;

namespace EnterpriseApiAutomationFramework.Core.Validators;

public static class AddMultipleMemberByExcelValidator
{
    public const string DefaultStatusColumn = "Status";

    public static void ValidateAllStatusesFromExcel(
        string fileName,
        string sheetName,
        string expectedStatus,
        string statusColumn = DefaultStatusColumn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedStatus);
        ArgumentException.ThrowIfNullOrWhiteSpace(statusColumn);

        var rows = ExcelReader.ReadSheet(fileName, sheetName);
        rows.Count.Should().BeGreaterThan(0,
            $"sheet '{sheetName}' in '{fileName}' must contain at least one response row to validate");

        var failures = CollectStatusFailures(rows, expectedStatus, statusColumn, rowLabelPrefix: "Excel row");
        failures.Should().BeEmpty(BuildFailureMessage(expectedStatus, failures));
    }

    public static void ValidateAllMemberStatusesFromResponse(
        string? responseContent,
        string expectedStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedStatus);

        var members = InfoResponseParse.TryAddMultipleMemberByExcelList(responseContent)
            ?? throw new InvalidOperationException(
                "Could not parse AddMultipleMemberByExcel response. Expected a JSON array.");

        members.Count.Should().BeGreaterThan(0,
            "AddMultipleMemberByExcel response must contain at least one member to validate");

        var failures = members
            .Where(m => !StatusMatches(m.Status, expectedStatus))
            .Select(m =>
            {
                var identifier = string.IsNullOrWhiteSpace(m.Email)
                    ? $"SerialNumber {m.SerialNumber}"
                    : $"SerialNumber {m.SerialNumber}, Email {m.Email}";
                return $"Member ({identifier}): expected '{expectedStatus}', actual '{m.Status}'";
            })
            .ToList();

        failures.Should().BeEmpty(BuildFailureMessage(expectedStatus, failures));
    }

    private static List<string> CollectStatusFailures(
        IReadOnlyList<Dictionary<string, string>> rows,
        string expectedStatus,
        string statusColumn,
        string rowLabelPrefix)
    {
        var failures = new List<string>();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 1;

            if (!row.TryGetValue(statusColumn, out var actualStatus) || string.IsNullOrWhiteSpace(actualStatus))
            {
                failures.Add($"{rowLabelPrefix} {rowNumber}: '{statusColumn}' is missing or empty");
                continue;
            }

            if (StatusMatches(actualStatus, expectedStatus))
                continue;

            var serial = row.TryGetValue("SerialNumber", out var sn) ? sn : rowNumber.ToString();
            var email = row.TryGetValue("Email", out var em) ? em : "-";
            failures.Add(
                $"{rowLabelPrefix} {rowNumber} (SerialNumber {serial}, Email {email}): expected '{expectedStatus}', actual '{actualStatus}'");
        }

        return failures;
    }

    // -------------------------------------------------------------------------
    //  NEW: per-role summary report + strict success assertion
    // -------------------------------------------------------------------------

    /// <summary>
    /// The only status that counts as a passing result for addMultipleMemberByExcel.
    /// </summary>
    public const string SuccessStatus = "Success! Member information added. MemberId:Success! Member information added.";

    /// <summary>
    /// Reads the response sheet, prints a grouped summary (status → count, serial nos, emails),
    /// then asserts that EVERY row has <see cref="SuccessStatus"/>.
    /// Fails with a human-readable report when any row has a different status.
    /// </summary>
    public static void ValidateAndReportMemberStatuses(
        string fileName,
        string sheetName,
        string statusColumn = DefaultStatusColumn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);

        var rows = ExcelReader.ReadSheet(fileName, sheetName);
        rows.Count.Should().BeGreaterThan(0,
            $"sheet '{sheetName}' in '{fileName}' must contain at least one response row to validate");

        // Group rows by their Status value
        var groups = rows
            .Select((row, idx) => new
            {
                RowNum   = idx + 1,
                Status   = row.TryGetValue(statusColumn, out var s) ? s?.Trim() ?? string.Empty : string.Empty,
                Serial   = row.TryGetValue("SerialNumber", out var sn) ? sn : (idx + 1).ToString(),
                Email    = row.TryGetValue("Email", out var em) ? em : "-"
            })
            .GroupBy(r => r.Status, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .ToList();

        // Build summary report
        var summary = new System.Text.StringBuilder();
        summary.AppendLine($"\n=== AddMultipleMemberByExcel Status Summary ({rows.Count} record(s)) ===");
        foreach (var group in groups)
        {
            var label = string.IsNullOrWhiteSpace(group.Key) ? "(empty)" : group.Key;
            summary.AppendLine($"\nStatus : {label}");
            summary.AppendLine($"Count  : {group.Count()}");
            foreach (var item in group)
                summary.AppendLine($"  Row {item.RowNum} | Serial: {item.Serial} | Email: {item.Email}");
        }
        summary.AppendLine("=== End of Summary ===");

        Console.WriteLine(summary.ToString());
        TestContext.Progress.WriteLine(summary.ToString());

        // Assert: ALL rows must be success
        var failures = rows
            .Select((row, idx) => new
            {
                RowNum = idx + 1,
                Status = row.TryGetValue(statusColumn, out var s) ? s?.Trim() ?? string.Empty : string.Empty,
                Serial = row.TryGetValue("SerialNumber", out var sn) ? sn : (idx + 1).ToString(),
                Email  = row.TryGetValue("Email", out var em) ? em : "-"
            })
            .Where(r => !StatusMatches(r.Status, SuccessStatus))
            .Select(r => $"  Row {r.RowNum} | Serial: {r.Serial} | Email: {r.Email} | Status: '{r.Status}'")
            .ToList();

        failures.Should().BeEmpty(
            $"Expected all {rows.Count} row(s) to have status '{SuccessStatus}' but {failures.Count} failed:\n" +
            string.Join(Environment.NewLine, failures));
    }

    private static bool StatusMatches(string? actual, string expected) =>
        string.Equals(actual?.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string BuildFailureMessage(string expectedStatus, IReadOnlyList<string> failures) =>
        $"All members must have status '{expectedStatus}'. Failed {failures.Count} of validated record(s):\n" +
        string.Join(Environment.NewLine, failures);
}
