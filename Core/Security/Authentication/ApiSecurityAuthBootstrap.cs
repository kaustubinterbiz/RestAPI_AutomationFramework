using ClosedXML.Excel;
using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Security.Authentication;

/// <summary>
/// Creates ApiSecurityAuthMatrix.xlsx with Phase-1 seed rows when the file is missing.
/// </summary>
public static class ApiSecurityAuthBootstrap
{
    public static void EnsureWorkbook()
    {
        var excelPath = GetWorkbookWritePath(ApiSecurityAuthConstants.MatrixExcelFile);
        if (File.Exists(excelPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(excelPath)!);
        using var workbook = new XLWorkbook();
        SeedAuthenticationSheet(workbook);
        workbook.SaveAs(excelPath);
    }

    private static void SeedAuthenticationSheet(XLWorkbook workbook)
    {
        var sheet = workbook.AddWorksheet(ApiSecurityAuthConstants.AuthenticationSheet);
        WriteHeader(
            sheet,
            ApiSecurityAuthConstants.TestCaseIdColumn,
            ApiSecurityAuthConstants.EndpointKeyColumn,
            ApiSecurityAuthConstants.HttpMethodColumn,
            ApiSecurityAuthConstants.ScenarioTypeColumn,
            ApiSecurityAuthConstants.RoleColumn,
            ApiSecurityAuthConstants.ExpectedStatusColumn,
            ApiSecurityAuthConstants.RequiresSessionColumn,
            ApiSecurityAuthConstants.HeaderKeysColumn,
            ApiSecurityAuthConstants.QueryParamKeysColumn,
            ApiSecurityAuthConstants.EnabledColumn,
            ApiSecurityAuthConstants.DescriptionColumn);

        // Phase 1: 3 existing GET endpoints × 7 JWT mutations
        var endpoints = new (string Key, string Query, string DescPrefix)[]
        {
            ("get", "-", "Session GetSessionInfo"),
            ("getExistingUser", "EmailId", "Account ExistingUser"),
            ("getCheckAvability", "-", "Member CheckEmailAvailibility")
        };

        var scenarios = new (string Type, string Desc)[]
        {
            (ApiSecurityAuthConstants.ScenarioNoAuthHeader, "Remove Authorization header entirely"),
            (ApiSecurityAuthConstants.ScenarioEmptyBearer, "Authorization: Bearer with empty token"),
            (ApiSecurityAuthConstants.ScenarioInvalidToken, "Garbage three-segment JWT"),
            (ApiSecurityAuthConstants.ScenarioMalformedToken, "Two-segment / non-base64 JWT"),
            (ApiSecurityAuthConstants.ScenarioExpiredToken, "Expired or file-based expired token"),
            (ApiSecurityAuthConstants.ScenarioTamperedToken, "After login: change payload claim, keep original signature"),
            (ApiSecurityAuthConstants.ScenarioMissingBearer, "Raw token without Bearer prefix")
        };

        var row = 2;
        foreach (var endpoint in endpoints)
        {
            var scenarioIndex = 1;
            foreach (var scenario in scenarios)
            {
                var testCaseId = $"AUTH-{endpoint.Key.ToUpperInvariant()}-{scenarioIndex:D2}";
                // ExistingUser often returns 400 (server exception) instead of 401 for bad JWTs.
                var expectedStatus =
                    string.Equals(endpoint.Key, "getExistingUser", StringComparison.OrdinalIgnoreCase)
                    && scenarioIndex is >= 3 and <= 6
                        ? "401|400"
                        : ApiSecurityAuthConstants.StatusUnauthorized.ToString();

                sheet.Cell(row, 1).Value = testCaseId;
                sheet.Cell(row, 2).Value = endpoint.Key;
                sheet.Cell(row, 3).Value = "GET";
                sheet.Cell(row, 4).Value = scenario.Type;
                sheet.Cell(row, 5).Value = ApiSecurityAuthConstants.DefaultRole;
                sheet.Cell(row, 6).Value = expectedStatus;
                sheet.Cell(row, 7).Value = "No";
                sheet.Cell(row, 8).Value = "-";
                sheet.Cell(row, 9).Value = endpoint.Query;
                sheet.Cell(row, 10).Value = "Yes";
                sheet.Cell(row, 11).Value = $"{endpoint.DescPrefix}: {scenario.Desc}";
                row++;
                scenarioIndex++;
            }
        }
    }

    private static void WriteHeader(IXLWorksheet sheet, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];
    }

    private static string GetWorkbookWritePath(string fileName)
    {
        var relativePath = Path.Combine("TestData", "UploadFiles", fileName);
        return ConfigReaderNew.ResolvePathForWrite(relativePath);
    }
}
