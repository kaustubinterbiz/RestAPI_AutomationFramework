using EnterpriseApiAutomationFramework.Core.Helpers;

namespace EnterpriseApiAutomationFramework.Core.Security.Patient;

public static class PatientSecurityMatrixReader
{
    public static IReadOnlyList<PatientSecurityEntry> GetAuthenticationRows(bool enabledOnly = true) =>
        ReadSheet(PatientSecurityConstants.AuthenticationSheet, "Authentication", enabledOnly);

    public static IReadOnlyList<PatientSecurityEntry> GetIdorRows(bool enabledOnly = true) =>
        ReadSheet(PatientSecurityConstants.IdorSheet, "IDOR", enabledOnly);

    public static IReadOnlyList<PatientSecurityEntry> GetInputValidationRows(bool enabledOnly = true) =>
        ReadSheet(PatientSecurityConstants.InputValidationSheet, "InputValidation", enabledOnly);

    public static IReadOnlyList<PatientSecurityEntry> GetAllPatientRows(bool enabledOnly = true) =>
        GetAuthenticationRows(enabledOnly)
            .Concat(GetIdorRows(enabledOnly))
            .Concat(GetInputValidationRows(enabledOnly))
            .ToList();

    public static PatientSecurityEntry? FindByTestCaseId(string testCaseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testCaseId);
        return GetAllPatientRows(enabledOnly: false)
            .FirstOrDefault(r => string.Equals(r.TestCaseId, testCaseId, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<PatientSecurityEntry> ReadSheet(
        string sheetName,
        string category,
        bool enabledOnly)
    {
        PatientSecurityBootstrap.EnsurePatientInfrastructure();

        var rows = ExcelReader.ReadSheet(PatientSecurityConstants.MatrixExcelFile, sheetName);
        var entries = new List<PatientSecurityEntry>();

        foreach (var row in rows)
        {
            var entry = PatientSecurityEntry.FromRow(row, category);
            if (entry == null || entry.SkipAutomation)
                continue;

            if (enabledOnly && entry.IsBaseline)
                continue;

            entries.Add(entry);
        }

        return entries;
    }
}
