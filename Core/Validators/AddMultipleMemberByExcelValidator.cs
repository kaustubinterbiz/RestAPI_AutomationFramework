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

    private static bool StatusMatches(string? actual, string expected) =>
        string.Equals(actual?.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string BuildFailureMessage(string expectedStatus, IReadOnlyList<string> failures) =>
        $"All members must have status '{expectedStatus}'. Failed {failures.Count} of validated record(s):\n" +
        string.Join(Environment.NewLine, failures);
}
