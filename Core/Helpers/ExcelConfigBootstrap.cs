using ClosedXML.Excel;
using EnterpriseApiAutomationFramework.Core.Configurations;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Creates Excel config workbooks from existing JSON when missing (one-time migration helper).
/// </summary>
public static class ExcelConfigBootstrap
{
    private const string AppSettingsFile = "appsettings.json";
    private const string EndpointJsonKey = "EndpointJson";
    private const string LoginJsonKey = "LoginJson";
    private const string BodyJsonKey = "JsonBody";

    public static void EnsureRequestEndPointWorkbook()
    {
        var excelPath = GetWorkbookWritePath(TestConfigDefaults.EndpointExcelFile);
        if (File.Exists(excelPath))
            return;

        ConfigReaderNew.LoadConfig(AppSettingsFile);
        var jsonPath = ConfigReaderNew.GetValue(EndpointJsonKey);
        if (string.IsNullOrWhiteSpace(jsonPath))
            throw new InvalidOperationException($"'{EndpointJsonKey}' is not set in '{AppSettingsFile}'.");

        var resolvedJsonPath = ConfigReaderNew.ResolvePathForRead(jsonPath);
        var jsonText = File.ReadAllText(resolvedJsonPath);
        var root = JsonNode.Parse(jsonText) as JsonObject
            ?? throw new InvalidOperationException($"Endpoint JSON root must be an object: '{resolvedJsonPath}'");

        Directory.CreateDirectory(Path.GetDirectoryName(excelPath)!);

        using var workbook = new XLWorkbook();

        var endpointsSheet = workbook.AddWorksheet(TestConfigDefaults.EndpointSheet);
        endpointsSheet.Cell(1, 1).Value = TestConfigDefaults.KeyColumn;
        endpointsSheet.Cell(1, 2).Value = TestConfigDefaults.ValueColumn;

        var row = 2;
        foreach (var property in root)
        {
            endpointsSheet.Cell(row, 1).Value = property.Key;
            endpointsSheet.Cell(row, 2).Value = property.Value?.ToString() ?? string.Empty;
            row++;
        }

        var responseSheet = workbook.AddWorksheet(TestConfigDefaults.EndpointResponseSheet);
        responseSheet.Cell(1, 1).Value = TestConfigDefaults.KeyColumn;
        responseSheet.Cell(1, 2).Value = TestConfigDefaults.ValueColumn;
        responseSheet.Cell(1, 3).Value = TestConfigDefaults.HttpStatusColumn;
        responseSheet.Cell(1, 4).Value = TestConfigDefaults.ResponseSnippetColumn;
        responseSheet.Cell(1, 5).Value = TestConfigDefaults.UpdatedAtColumn;

        workbook.SaveAs(excelPath);
    }

    public static void EnsureLoginRequestWorkbook()
    {
        var excelPath = GetWorkbookWritePath(TestConfigDefaults.LoginExcelFile);
        if (File.Exists(excelPath))
            return;

        ConfigReaderNew.LoadConfig(AppSettingsFile);
        var jsonPath = ConfigReaderNew.GetValue(LoginJsonKey);
        if (string.IsNullOrWhiteSpace(jsonPath))
            throw new InvalidOperationException($"'{LoginJsonKey}' is not set in '{AppSettingsFile}'.");

        var resolvedJsonPath = ConfigReaderNew.ResolvePathForRead(jsonPath);
        var jsonText = File.ReadAllText(resolvedJsonPath);
        var root = JsonNode.Parse(jsonText, documentOptions: new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        }) as JsonObject
            ?? throw new InvalidOperationException($"Login JSON root must be an object: '{resolvedJsonPath}'");

        Directory.CreateDirectory(Path.GetDirectoryName(excelPath)!);

        using var workbook = new XLWorkbook();

        var rolesSheet = workbook.AddWorksheet(TestConfigDefaults.LoginRolesSheet);
        rolesSheet.Cell(1, 1).Value = TestConfigDefaults.RoleColumn;
        for (var c = 0; c < TestConfigDefaults.LoginCredentialColumns.Count; c++)
            rolesSheet.Cell(1, c + 2).Value = TestConfigDefaults.LoginCredentialColumns[c];

        var row = 2;
        foreach (var roleEntry in root)
        {
            if (roleEntry.Value is not JsonObject roleObject)
                continue;

            rolesSheet.Cell(row, 1).Value = roleEntry.Key;
            for (var c = 0; c < TestConfigDefaults.LoginCredentialColumns.Count; c++)
            {
                var field = TestConfigDefaults.LoginCredentialColumns[c];
                rolesSheet.Cell(row, c + 2).Value =
                    roleObject[field]?.ToString() ?? string.Empty;
            }

            row++;
        }

        var responseSheet = workbook.AddWorksheet(TestConfigDefaults.LoginResponseSheet);
        responseSheet.Cell(1, 1).Value = TestConfigDefaults.RoleColumn;
        responseSheet.Cell(1, 2).Value = TestConfigDefaults.HttpStatusColumn;
        responseSheet.Cell(1, 3).Value = TestConfigDefaults.TokenSnippetColumn;
        responseSheet.Cell(1, 4).Value = TestConfigDefaults.UpdatedAtColumn;

        workbook.SaveAs(excelPath);
    }

    /// <summary>
    /// Ensures LoginRequest.xlsx RoleGroups sheet exists and AddMemberRole rows are seeded/migrated.
    /// </summary>
    public static void EnsureRoleGroupsSheet()
    {
        EnsureLoginRequestWorkbook();

        var excelPath = GetWorkbookWritePath(TestConfigDefaults.LoginExcelFile);
        using var workbook = new XLWorkbook(excelPath);

        EnsureRoleAliases(workbook);

        var sheet = workbook.TryGetWorksheet(TestConfigDefaults.RoleGroupsSheet, out var roleGroupsSheet)
            ? roleGroupsSheet
            : workbook.AddWorksheet(TestConfigDefaults.RoleGroupsSheet);

        EnsureRoleGroupsHeader(sheet);

        var existingAddMemberRows = ReadParentGroupRows(sheet, TestConfigDefaults.DefaultAddMemberRoleGroup);
        if (existingAddMemberRows.Count == 0)
        {
            AppendParentRoleGroupRows(sheet, TestConfigDefaults.DefaultAddMemberRoleGroup, TestConfigDefaults.DefaultAddMemberChildRoles);
        }
        else if (existingAddMemberRows.Any(r =>
                     string.Equals(r.ChildRole, "AdminRole", StringComparison.OrdinalIgnoreCase)))
        {
            SyncParentRoleGroup(sheet, TestConfigDefaults.DefaultAddMemberRoleGroup, TestConfigDefaults.DefaultAddMemberChildRoles);
        }

        workbook.Save();
    }

    private static List<RoleGroupEntry> ReadParentGroupRows(IXLWorksheet sheet, string parentRole)
    {
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var rows = new List<RoleGroupEntry>();

        for (var r = 2; r <= lastRow; r++)
        {
            var parent = sheet.Cell(r, 1).GetString().Trim();
            if (!string.Equals(parent, parentRole, StringComparison.OrdinalIgnoreCase))
                continue;

            var entry = RoleGroupEntry.FromRow(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [TestConfigDefaults.ParentRoleColumn] = parent,
                [TestConfigDefaults.ChildRoleColumn] = sheet.Cell(r, 2).GetString().Trim(),
                [TestConfigDefaults.ExecutionOrderColumn] = sheet.Cell(r, 3).GetString().Trim(),
                [TestConfigDefaults.EnabledColumn] = sheet.Cell(r, 4).GetString().Trim()
            });

            if (entry != null)
                rows.Add(entry);
        }

        return rows;
    }

    private static void AppendParentRoleGroupRows(
        IXLWorksheet sheet,
        string parentRole,
        IReadOnlyList<(string Role, int Order)> childRoles)
    {
        var row = (sheet.LastRowUsed()?.RowNumber() ?? 1) + 1;
        if (row == 2 && IsRoleGroupsDataEmpty(sheet))
            row = 2;

        foreach (var (role, order) in childRoles)
        {
            sheet.Cell(row, 1).Value = parentRole;
            sheet.Cell(row, 2).Value = role;
            sheet.Cell(row, 3).Value = order;
            sheet.Cell(row, 4).Value = "Yes";
            row++;
        }
    }

    private static bool IsRoleGroupsDataEmpty(IXLWorksheet sheet)
    {
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        if (lastRow < 2)
            return true;

        for (var r = 2; r <= lastRow; r++)
        {
            if (!string.IsNullOrWhiteSpace(sheet.Cell(r, 1).GetString())
                || !string.IsNullOrWhiteSpace(sheet.Cell(r, 2).GetString()))
            {
                return false;
            }
        }

        return true;
    }

    private static void EnsureRoleGroupsHeader(IXLWorksheet sheet)
    {
        sheet.Cell(1, 1).Value = TestConfigDefaults.ParentRoleColumn;
        sheet.Cell(1, 2).Value = TestConfigDefaults.ChildRoleColumn;
        sheet.Cell(1, 3).Value = TestConfigDefaults.ExecutionOrderColumn;
        sheet.Cell(1, 4).Value = TestConfigDefaults.EnabledColumn;
    }

    private static void SyncParentRoleGroup(
        IXLWorksheet sheet,
        string parentRole,
        IReadOnlyList<(string Role, int Order)> childRoles)
    {
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var preservedRows = new List<Dictionary<string, string>>();

        if (lastRow >= 2)
        {
            for (var r = 2; r <= lastRow; r++)
            {
                var parent = sheet.Cell(r, 1).GetString().Trim();
                if (string.Equals(parent, parentRole, StringComparison.OrdinalIgnoreCase))
                    continue;

                preservedRows.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [TestConfigDefaults.ParentRoleColumn] = parent,
                    [TestConfigDefaults.ChildRoleColumn] = sheet.Cell(r, 2).GetString().Trim(),
                    [TestConfigDefaults.ExecutionOrderColumn] = sheet.Cell(r, 3).GetString().Trim(),
                    [TestConfigDefaults.EnabledColumn] = sheet.Cell(r, 4).GetString().Trim()
                });
            }
        }

        if (lastRow >= 2)
            sheet.Range(2, 1, lastRow, 4).Clear(XLClearOptions.Contents);

        var row = 2;
        foreach (var (role, order) in childRoles)
        {
            sheet.Cell(row, 1).Value = parentRole;
            sheet.Cell(row, 2).Value = role;
            sheet.Cell(row, 3).Value = order;
            sheet.Cell(row, 4).Value = "Yes";
            row++;
        }

        foreach (var preserved in preservedRows)
        {
            sheet.Cell(row, 1).Value = preserved[TestConfigDefaults.ParentRoleColumn];
            sheet.Cell(row, 2).Value = preserved[TestConfigDefaults.ChildRoleColumn];
            sheet.Cell(row, 3).Value = preserved[TestConfigDefaults.ExecutionOrderColumn];
            sheet.Cell(row, 4).Value = string.IsNullOrWhiteSpace(preserved[TestConfigDefaults.EnabledColumn])
                ? "Yes"
                : preserved[TestConfigDefaults.EnabledColumn];
            row++;
        }
    }

    private static void EnsureRoleAliases(IXLWorkbook workbook)
    {
        if (!workbook.TryGetWorksheet(TestConfigDefaults.LoginRolesSheet, out var rolesSheet))
            return;

        var range = rolesSheet.RangeUsed();
        if (range == null || range.RowCount() < 2)
            return;

        var headers = Enumerable.Range(1, range.Row(1).CellCount())
            .Select(c => rolesSheet.Cell(1, c).GetString().Trim())
            .ToList();

        var roleCol = headers.FindIndex(h =>
            string.Equals(h, TestConfigDefaults.RoleColumn, StringComparison.OrdinalIgnoreCase));
        if (roleCol < 0)
            return;

        var existingRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var r = 2; r <= range.RowCount(); r++)
        {
            var roleName = rolesSheet.Cell(r, roleCol + 1).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(roleName))
                existingRoles.Add(roleName);
        }

        CopyRoleAliasIfMissing(rolesSheet, headers, existingRoles, "OrganizationRole", "HospitalRole");
    }

    private static void CopyRoleAliasIfMissing(
        IXLWorksheet rolesSheet,
        IReadOnlyList<string> headers,
        ISet<string> existingRoles,
        string aliasRole,
        string sourceRole)
    {
        if (existingRoles.Contains(aliasRole) || !existingRoles.Contains(sourceRole))
            return;

        var sourceRow = FindRoleRow(rolesSheet, headers, sourceRole);
        if (sourceRow == null)
            return;

        var nextRow = rolesSheet.LastRowUsed()?.RowNumber() + 1 ?? 2;
        for (var c = 0; c < headers.Count; c++)
        {
            var header = headers[c];
            rolesSheet.Cell(nextRow, c + 1).Value = string.Equals(header, TestConfigDefaults.RoleColumn, StringComparison.OrdinalIgnoreCase)
                ? aliasRole
                : sourceRow[c];
        }

        existingRoles.Add(aliasRole);
    }

    private static IReadOnlyList<string>? FindRoleRow(
        IXLWorksheet rolesSheet,
        IReadOnlyList<string> headers,
        string roleName)
    {
        var roleCol = headers.ToList().FindIndex(h =>
            string.Equals(h, TestConfigDefaults.RoleColumn, StringComparison.OrdinalIgnoreCase));
        if (roleCol < 0)
            return null;

        var lastRow = rolesSheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= lastRow; r++)
        {
            if (!string.Equals(rolesSheet.Cell(r, roleCol + 1).GetString().Trim(), roleName, StringComparison.OrdinalIgnoreCase))
                continue;

            var values = new string[headers.Count];
            for (var c = 0; c < headers.Count; c++)
                values[c] = rolesSheet.Cell(r, c + 1).GetString();

            return values;
        }

        return null;
    }

    public static void EnsureRequestBodyWorkbook()
    {
        var excelPath = GetWorkbookWritePath(TestConfigDefaults.BodyExcelFile);
        if (File.Exists(excelPath))
            return;

        ConfigReaderNew.LoadConfig(AppSettingsFile);
        var jsonPath = ConfigReaderNew.GetValue(BodyJsonKey);
        if (string.IsNullOrWhiteSpace(jsonPath))
            throw new InvalidOperationException($"'{BodyJsonKey}' is not set in '{AppSettingsFile}'.");

        var resolvedJsonPath = ConfigReaderNew.ResolvePathForRead(jsonPath);
        var jsonText = File.ReadAllText(resolvedJsonPath);
        var root = JsonNode.Parse(jsonText, documentOptions: new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        }) as JsonObject
            ?? throw new InvalidOperationException($"Request body JSON root must be an object: '{resolvedJsonPath}'");

        Directory.CreateDirectory(Path.GetDirectoryName(excelPath)!);

        using var workbook = new XLWorkbook();

        foreach (var bodyEntry in root)
        {
            if (bodyEntry.Value is not JsonObject bodyObject)
                continue;

            var sheet = workbook.AddWorksheet(SanitizeSheetName(bodyEntry.Key));
            sheet.Cell(1, 1).Value = TestConfigDefaults.FieldColumn;
            sheet.Cell(1, 2).Value = TestConfigDefaults.ValueColumn;

            if (IsFlatBodyObject(bodyObject))
            {
                var row = 2;
                foreach (var field in bodyObject)
                {
                    sheet.Cell(row, 1).Value = field.Key;
                    sheet.Cell(row, 2).Value = field.Value?.ToString() ?? string.Empty;
                    row++;
                }
            }
            else
            {
                sheet.Cell(2, 1).Value = TestConfigDefaults.BodyRawJsonMarker;
                sheet.Cell(2, 2).Value = bodyObject.ToJsonString();
            }
        }

        var responseSheet = workbook.AddWorksheet(TestConfigDefaults.BodyResponseSheet);
        responseSheet.Cell(1, 1).Value = TestConfigDefaults.KeyColumn;
        responseSheet.Cell(1, 2).Value = TestConfigDefaults.HttpStatusColumn;
        responseSheet.Cell(1, 3).Value = TestConfigDefaults.ResponseSnippetColumn;
        responseSheet.Cell(1, 4).Value = TestConfigDefaults.UpdatedAtColumn;

        workbook.SaveAs(excelPath);
    }

    private static bool IsFlatBodyObject(JsonObject bodyObject) =>
        bodyObject.All(property => property.Value is not (JsonObject or JsonArray));

    private static string SanitizeSheetName(string sheetName)
    {
        var invalid = new[] { '\\', '/', '?', '*', '[', ']', ':' };
        var sanitized = sheetName;
        foreach (var ch in invalid)
            sanitized = sanitized.Replace(ch, '_');

        return sanitized.Length > 31 ? sanitized[..31] : sanitized;
    }

    private static string GetWorkbookWritePath(string fileName)
    {
        var relativePath = Path.Combine("TestData", "UploadFiles", fileName);
        return ConfigReaderNew.ResolvePathForWrite(relativePath);
    }
}
