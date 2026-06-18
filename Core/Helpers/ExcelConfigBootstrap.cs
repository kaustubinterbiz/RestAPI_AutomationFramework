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
