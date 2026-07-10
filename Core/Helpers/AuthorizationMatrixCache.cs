using ClosedXML.Excel;
using EnterpriseApiAutomationFramework.Core.Authorization;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// In-memory cache for AuthorizationMatrix.xlsx to avoid file-lock errors when Excel is open.
/// Loaded once per test run via <see cref="Load"/>.
/// </summary>
internal static class AuthorizationMatrixCache
{
    private static readonly object Gate = new();

    private static bool _loaded;
    private static List<Dictionary<string, string>> _endpointAccessRows = [];
    private static List<Dictionary<string, string>> _tokenScenarioRows = [];
    private static List<Dictionary<string, string>> _permissionRows = [];

    public static void Load()
    {
        lock (Gate)
        {
            if (_loaded)
                return;

            AuthorizationConfigBootstrap.EnsureAuthorizationMatrixWorkbook();

            _endpointAccessRows = ReadSheetWithSharedAccess(AuthorizationConstants.EndpointAccessSheet);
            _tokenScenarioRows = ReadSheetWithSharedAccess(AuthorizationConstants.TokenScenariosSheet);
            _permissionRows = ReadSheetWithSharedAccess(AuthorizationConstants.PermissionsSheet);
            _loaded = true;
        }
    }

    public static IReadOnlyList<Dictionary<string, string>> GetEndpointAccessRows() =>
        GetRows(ref _loaded, _endpointAccessRows);

    public static IReadOnlyList<Dictionary<string, string>> GetTokenScenarioRows() =>
        GetRows(ref _loaded, _tokenScenarioRows);

    public static IReadOnlyList<Dictionary<string, string>> GetPermissionRows() =>
        GetRows(ref _loaded, _permissionRows);

    private static IReadOnlyList<Dictionary<string, string>> GetRows(
        ref bool loaded,
        List<Dictionary<string, string>> rows)
    {
        if (!loaded)
            Load();

        return rows;
    }

    private static List<Dictionary<string, string>> ReadSheetWithSharedAccess(string sheetName)
    {
        var filePath = FileUploadHelper.GetFilePath(AuthorizationConstants.MatrixExcelFile);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                return ReadSheetFromFile(filePath, sheetName);
            }
            catch (IOException) when (attempt < 3)
            {
                Thread.Sleep(200 * attempt);
            }
        }

        return ReadSheetFromFile(filePath, sheetName);
    }

    private static List<Dictionary<string, string>> ReadSheetFromFile(string filePath, string sheetName)
    {
        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);

        using var workbook = new XLWorkbook(stream);

        if (!workbook.TryGetWorksheet(sheetName, out var sheet))
        {
            throw new InvalidOperationException(
                $"Sheet '{sheetName}' not found in '{AuthorizationConstants.MatrixExcelFile}'. " +
                $"Available sheets: {string.Join(", ", workbook.Worksheets.Select(ws => ws.Name))}");
        }

        var rows = new List<Dictionary<string, string>>();
        var range = sheet.RangeUsed();
        if (range == null)
            return rows;

        var headers = Enumerable.Range(1, range.Row(1).CellCount())
            .Select(c => GetCellValue(range.Row(1).Cell(c)))
            .ToList();

        for (var r = 2; r <= range.RowCount(); r++)
        {
            var dataRow = range.Row(r);
            if (dataRow.Cells().All(c => c.IsEmpty()))
                continue;

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Count; c++)
                dict[headers[c]] = GetCellValue(dataRow.Cell(c + 1));

            rows.Add(dict);
        }

        return rows;
    }

    private static string GetCellValue(IXLCell cell)
    {
        if (cell.IsEmpty())
            return string.Empty;

        return cell.Value.ToString()?.Trim() ?? string.Empty;
    }
}
