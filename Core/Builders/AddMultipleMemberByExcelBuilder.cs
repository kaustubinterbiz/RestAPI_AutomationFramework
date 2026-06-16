using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Models.Request;
using System.Globalization;

namespace EnterpriseApiAutomationFramework.Core.Builders;

/// <summary>
/// Builds <see cref="AddMultipleMemberByExcelRequest"/> from an Excel sheet.
/// Supports both header styles: "SerialNumber" and "Serial Number".
/// </summary>
public static class AddMultipleMemberByExcelBuilder
{
    public static AddMultipleMemberByExcelRequest BuildFromExcel(
        string fileName,
        string sheetName,
        string businessUnitId,
        bool addExistingUser = false,
        long defaultFaxNumber = 2222222222)
    {
        var rows = ExcelReader.ReadSheet(fileName, sheetName);

        if (rows.Count == 0)
        {
            throw new InvalidOperationException(
                $"No data rows found in '{fileName}' sheet '{sheetName}'.");
        }

        return new AddMultipleMemberByExcelRequest
        {
            BusinessUnitID = businessUnitId,
            AddExistingUser = addExistingUser,
            MemberList = rows.Select(MapRow(defaultFaxNumber)).ToList()
        };
    }

    private static Func<Dictionary<string, string>, MemberItem> MapRow(long defaultFaxNumber) =>
        row => new MemberItem
        {
            SerialNumber      = ParseInt(row, "SerialNumber", "Serial Number"),
            FirstName         = GetString(row, "FirstName", "First Name"),
            MiddleName        = GetString(row, "MiddleName", "Middle Name"),
            LastName          = GetString(row, "LastName", "Last Name"),
            RoleName          = GetString(row, "RoleName", "Role Name"),
            WorkNumber        = ParseLong(row, "WorkNumber", "Work Number"),
            FaxNumber         = TryGetString(row, "FaxNumber", "Fax Number") is { Length: > 0 } fax
                                    ? ParseLong(fax) : defaultFaxNumber,
            MobileNumber      = ParseLong(row, "MobileNumber", "Mobile Number"),
            IsPrimaryMember   = GetString(row, "IsPrimaryMember", "Is Primary Member"),
            IsSecondaryMember = GetString(row, "IsSecondaryMember", "Is Secondary Member"),
            Email             = GetString(row, "Email"),
            Designation       = GetString(row, "Designation")
        };

    private static string GetString(Dictionary<string, string> row, params string[] columnNames)
    {
        var value = TryGetString(row, columnNames);
        if (value == null)
        {
            throw new KeyNotFoundException(
                $"Column not found. Tried: {string.Join(", ", columnNames)}. " +
                $"Available in Excel: {string.Join(", ", row.Keys)}");
        }

        return value;
    }

    private static string? TryGetString(Dictionary<string, string> row, params string[] columnNames)
    {
        foreach (var name in columnNames)
        {
            if (row.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }

    private static int ParseInt(Dictionary<string, string> row, params string[] columnNames)
        => (int)ParseLong(row, columnNames);

    private static long ParseLong(Dictionary<string, string> row, params string[] columnNames)
        => ParseLong(GetString(row, columnNames));

    private static long ParseLong(string? raw)
    {
        raw = raw?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(raw))
            return 0;

        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole))
            return whole;

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return (long)number;

        throw new FormatException($"Cannot parse '{raw}' as a number.");
    }
}
