using ClosedXML.Excel;
using EnterpriseApiAutomationFramework.Core.Authorization;
using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Creates AuthorizationMatrix.xlsx when missing (seed data for authorization tests).
/// </summary>
public static class AuthorizationConfigBootstrap
{
    public static void EnsureAuthorizationMatrixWorkbook()
    {
        var excelPath = GetWorkbookWritePath(AuthorizationConstants.MatrixExcelFile);
        if (File.Exists(excelPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(excelPath)!);
        using var workbook = new XLWorkbook();

        SeedEndpointAccessSheet(workbook);
        SeedTokenScenariosSheet(workbook);
        SeedPermissionsSheet(workbook);

        workbook.SaveAs(excelPath);
    }

    private static void SeedEndpointAccessSheet(XLWorkbook workbook)
    {
        var sheet = workbook.AddWorksheet(AuthorizationConstants.EndpointAccessSheet);
        WriteHeader(sheet, 1,
            AuthorizationConstants.EndpointKeyColumn,
            AuthorizationConstants.HttpMethodColumn,
            AuthorizationConstants.RoleColumn,
            AuthorizationConstants.ExpectedStatusColumn,
            AuthorizationConstants.RequiresSessionColumn,
            AuthorizationConstants.HeaderKeysColumn,
            AuthorizationConstants.UrlPlaceholderKeysColumn,
            AuthorizationConstants.TargetValueColumn,
            AuthorizationConstants.QueryParamKeysColumn,
            AuthorizationConstants.EnabledColumn,
            AuthorizationConstants.DescriptionColumn);

        var rows = new (string Endpoint, string Method, string Role, int Status, string Session, string Headers, string UrlKeys, string Target, string Query, string Desc)[]
        {
            ("get", "GET", "SuperAdmin", 200, "No", "-", "-", "-", "-", "Session info for SuperAdmin"),
            ("get", "GET", "HospitalRole", 200, "No", "-", "-", "-", "-", "Session info for HospitalRole"),
            ("get", "GET", "OrganizationRole", 200, "No", "-", "-", "-", "-", "Session info for OrganizationRole"),
            ("getExistingUser", "GET", "SuperAdmin", 200, "Yes", "CacheId", "-", "-", "EmailId", "Read existing user as SuperAdmin"),
            ("getExistingUser", "GET", "HospitalRole", 200, "Yes", "CacheId", "-", "-", "EmailId", "Read existing user as HospitalRole"),
            ("getExistingUser", "GET", "OrganizationRole", 200, "Yes", "CacheId", "-", "-", "EmailId", "Read existing user as OrganizationRole"),
            ("getExistingUser", "GET", AuthorizationConstants.GuestRole, 401, "No", "-", "-", "-", "EmailId", "Guest without login")
        };

        var row = 2;
        foreach (var entry in rows)
        {
            sheet.Cell(row, 1).Value = entry.Endpoint;
            sheet.Cell(row, 2).Value = entry.Method;
            sheet.Cell(row, 3).Value = entry.Role;
            sheet.Cell(row, 4).Value = entry.Status;
            sheet.Cell(row, 5).Value = entry.Session;
            sheet.Cell(row, 6).Value = entry.Headers;
            sheet.Cell(row, 7).Value = entry.UrlKeys;
            sheet.Cell(row, 8).Value = entry.Target;
            sheet.Cell(row, 9).Value = entry.Query;
            sheet.Cell(row, 10).Value = "Yes";
            sheet.Cell(row, 11).Value = entry.Desc;
            row++;
        }
    }

    private static void SeedTokenScenariosSheet(XLWorkbook workbook)
    {
        var sheet = workbook.AddWorksheet(AuthorizationConstants.TokenScenariosSheet);
        WriteHeader(sheet, 1,
            AuthorizationConstants.ScenarioTypeColumn,
            AuthorizationConstants.RoleColumn,
            AuthorizationConstants.EndpointKeyColumn,
            AuthorizationConstants.ExpectedStatusColumn,
            AuthorizationConstants.HeaderModeColumn,
            AuthorizationConstants.BaseUrlColumn,
            AuthorizationConstants.EnabledColumn,
            AuthorizationConstants.DescriptionColumn);

        var rows = new (string Scenario, string Role, string Endpoint, int Status, string HeaderMode, string BaseUrl, string Desc)[]
        {
            ("ValidToken", "SuperAdmin", "post", 200, "Bearer", "Auth", "Valid JWT after login on Auth host"),
            ("InvalidToken", "SuperAdmin", "post", 400, "Bearer", "Auth", "Tampered JWT on Auth token endpoint"),
            ("ExpiredToken", "SuperAdmin", "post", 400, "Bearer", "Auth", "Expired JWT on Auth token endpoint"),
            ("EmptyToken", "-", "get", 401, "None", "Api", "No Authorization header on Api session endpoint"),
            ("TamperedToken", "SuperAdmin", "post", 400, "Bearer", "Auth", "Modified JWT on Auth token endpoint"),
            ("MissingBearer", "SuperAdmin", "post", 400, "Raw", "Auth", "Token without Bearer prefix on Auth host")
        };

        var row = 2;
        foreach (var entry in rows)
        {
            sheet.Cell(row, 1).Value = entry.Scenario;
            sheet.Cell(row, 2).Value = entry.Role;
            sheet.Cell(row, 3).Value = entry.Endpoint;
            sheet.Cell(row, 4).Value = entry.Status;
            sheet.Cell(row, 5).Value = entry.HeaderMode;
            sheet.Cell(row, 6).Value = entry.BaseUrl;
            sheet.Cell(row, 7).Value = "Yes";
            sheet.Cell(row, 8).Value = entry.Desc;
            row++;
        }
    }

    private static void SeedPermissionsSheet(XLWorkbook workbook)
    {
        var sheet = workbook.AddWorksheet(AuthorizationConstants.PermissionsSheet);
        WriteHeader(sheet, 1,
            AuthorizationConstants.RoleColumn,
            AuthorizationConstants.PermissionColumn,
            AuthorizationConstants.EndpointKeyColumn,
            AuthorizationConstants.HttpMethodColumn,
            AuthorizationConstants.AllowedColumn,
            AuthorizationConstants.RequiresSessionColumn,
            AuthorizationConstants.HeaderKeysColumn,
            "BodyKey",
            AuthorizationConstants.EnabledColumn);

        var rows = new (string Role, string Permission, string Endpoint, string Method, string Allowed, string Session, string Headers, string Body)[]
        {
            ("SuperAdmin", "Read", "getExistingUser", "GET", "Yes", "Yes", "CacheId", "-"),
            ("SuperAdmin", "Create", "post_Register", "POST", "Yes", "Yes", "CacheId", "register_Body"),
            ("HospitalRole", "Read", "getExistingUser", "GET", "Yes", "Yes", "CacheId", "-"),
            ("HospitalRole", "Create", "post_Register", "POST", "Yes", "Yes", "CacheId", "register_Body"),
            ("OrganizationRole", "Read", "getExistingUser", "GET", "Yes", "Yes", "CacheId", "-"),
            ("OrganizationRole", "Create", "post_Register", "POST", "Yes", "Yes", "CacheId", "register_Body")
        };

        var row = 2;
        foreach (var entry in rows)
        {
            sheet.Cell(row, 1).Value = entry.Role;
            sheet.Cell(row, 2).Value = entry.Permission;
            sheet.Cell(row, 3).Value = entry.Endpoint;
            sheet.Cell(row, 4).Value = entry.Method;
            sheet.Cell(row, 5).Value = entry.Allowed;
            sheet.Cell(row, 6).Value = entry.Session;
            sheet.Cell(row, 7).Value = entry.Headers;
            sheet.Cell(row, 8).Value = entry.Body;
            sheet.Cell(row, 9).Value = "Yes";
            row++;
        }
    }

    private static void WriteHeader(IXLWorksheet sheet, int row, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(row, i + 1).Value = headers[i];
    }

    private static string GetWorkbookWritePath(string fileName)
    {
        var relativePath = Path.Combine("TestData", "UploadFiles", fileName);
        return ConfigReaderNew.ResolvePathForWrite(relativePath);
    }
}
