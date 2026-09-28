using EnterpriseApiAutomationFramework.Core.Helpers;

namespace EnterpriseApiAutomationFramework.Core.Security.Authentication;

/// <summary>
/// Reads enabled Authentication rows from ApiSecurityAuthMatrix.xlsx.
/// </summary>
public static class ApiSecurityAuthMatrixReader
{
    public static IReadOnlyList<ApiSecurityAuthEntry> GetAuthenticationRows(bool enabledOnly = true)
    {
        ApiSecurityAuthBootstrap.EnsureWorkbook();

        var rows = ExcelReader.ReadSheet(
            ApiSecurityAuthConstants.MatrixExcelFile,
            ApiSecurityAuthConstants.AuthenticationSheet);

        var entries = new List<ApiSecurityAuthEntry>();

        foreach (var row in rows)
        {
            var entry = ApiSecurityAuthEntry.FromRow(row);
            if (entry == null)
                continue;

            if (enabledOnly && !IsEnabled(row.GetValueOrDefault(ApiSecurityAuthConstants.EnabledColumn, "Yes")))
                continue;

            entries.Add(entry);
        }

        return entries;
    }

    private static bool IsEnabled(string? value) =>
        string.Equals(value, "Yes", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "Y", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "True", StringComparison.OrdinalIgnoreCase)
        || value == "1"
        || string.IsNullOrWhiteSpace(value);
}
