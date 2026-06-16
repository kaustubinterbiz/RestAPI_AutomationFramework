using ClosedXML.Excel;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Read and write Excel files stored in TestData/UploadFiles/.
/// Pass only the filename and sheet name — folder path is auto-resolved.
/// First row of every sheet is always treated as the header row.
/// </summary>
public static class ExcelReader
{
    // =========================================================================
    //  READ
    // =========================================================================

    /// <summary>
    /// Reads all data rows from the given sheet.
    /// Returns a list of dictionaries where Key = column header, Value = cell value.
    /// </summary>
    public static List<Dictionary<string, string>> ReadSheet(string fileName, string sheetName)
    {
        var filePath = FileUploadHelper.GetFilePath(fileName);
        using var workbook = new XLWorkbook(filePath);
        var sheet = GetSheet(workbook, fileName, sheetName);

        var rows  = new List<Dictionary<string, string>>();
        var range = sheet.RangeUsed();
        if (range == null) return rows;

        var headers = ReadHeaders(range);

        for (int r = 2; r <= range.RowCount(); r++)
        {
            var dataRow = range.Row(r);
            if (dataRow.Cells().All(c => c.IsEmpty())) continue;

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int c = 0; c < headers.Count; c++)
                dict[headers[c]] = GetCellValue(dataRow.Cell(c + 1));

            rows.Add(dict);
        }

        return rows;
    }

    /// <summary>
    /// Returns a single row by 1-based data row index (row 1 = first data row after header).
    /// </summary>
    public static Dictionary<string, string> GetRow(string fileName, string sheetName, int rowIndex)
    {
        var rows = ReadSheet(fileName, sheetName);

        if (rowIndex < 1 || rowIndex > rows.Count)
            throw new ArgumentOutOfRangeException(nameof(rowIndex),
                $"Row {rowIndex} does not exist. Sheet '{sheetName}' has {rows.Count} data row(s).");

        return rows[rowIndex - 1];
    }

    private static string GetCellValue(IXLCell cell)
    {
        if (cell.IsEmpty()) return string.Empty;
        return cell.Value.ToString()?.Trim() ?? string.Empty;
    }
    /// <summary>
    /// Returns all values from a single column by header name.
    /// </summary>
    public static List<string> GetColumn(string fileName, string sheetName, string columnHeader)
    {
        var rows = ReadSheet(fileName, sheetName);

        if (rows.Count > 0 && !rows[0].ContainsKey(columnHeader))
            throw new KeyNotFoundException(
                $"Column '{columnHeader}' not found in sheet '{sheetName}'. " +
                $"Available columns: {string.Join(", ", rows[0].Keys)}");

        return rows.Select(r => r.TryGetValue(columnHeader, out var v) ? v : string.Empty).ToList();
    }

    /// <summary>Returns the names of all sheets in the workbook.</summary>
    public static List<string> GetSheetNames(string fileName)
    {
        var filePath = FileUploadHelper.GetFilePath(fileName);
        using var workbook = new XLWorkbook(filePath);
        return workbook.Worksheets.Select(ws => ws.Name).ToList();
    }

    // =========================================================================
    //  UPDATE
    // =========================================================================

    /// <summary>
    /// Updates specific columns in a row identified by its 1-based data row index.
    /// Only the columns present in <paramref name="columnUpdates"/> are changed; others are left as-is.
    /// </summary>
    /// <param name="rowIndex">1-based data row index (row 1 = first row after header).</param>
    /// <param name="columnUpdates">Dictionary of column-header → new value.</param>
    public static void UpdateRow(
        string fileName,
        string sheetName,
        int rowIndex,
        Dictionary<string, string> columnUpdates)
    {
        var filePath = FileUploadHelper.GetFilePath(fileName);
        using var workbook = new XLWorkbook(filePath);
        var sheet = GetSheet(workbook, fileName, sheetName);
        var range = sheet.RangeUsed()
            ?? throw new InvalidOperationException($"Sheet '{sheetName}' is empty.");

        var headers    = ReadHeaders(range);
        var sheetRow   = rowIndex + 1;  // +1 because row 1 = header in the sheet

        if (rowIndex < 1 || rowIndex > range.RowCount() - 1)
            throw new ArgumentOutOfRangeException(nameof(rowIndex),
                $"Row {rowIndex} does not exist in sheet '{sheetName}'.");

        foreach (var (column, value) in columnUpdates)
        {
            var colIndex = headers.FindIndex(h =>
                string.Equals(h, column, StringComparison.OrdinalIgnoreCase));

            if (colIndex == -1)
                throw new KeyNotFoundException(
                    $"Column '{column}' not found. Available: {string.Join(", ", headers)}");

            sheet.Cell(sheetRow, colIndex + 1).Value = value;
        }

        workbook.Save();
    }

    /// <summary>
    /// Finds the first row where <paramref name="searchColumn"/> equals <paramref name="searchValue"/>
    /// and updates the columns specified in <paramref name="columnUpdates"/>.
    /// Throws when no matching row is found.
    /// </summary>
    public static void UpdateRowWhere(
        string fileName,
        string sheetName,
        string searchColumn,
        string searchValue,
        Dictionary<string, string> columnUpdates)
    {
        var filePath = FileUploadHelper.GetFilePath(fileName);
        using var workbook = new XLWorkbook(filePath);
        var sheet = GetSheet(workbook, fileName, sheetName);
        var range = sheet.RangeUsed()
            ?? throw new InvalidOperationException($"Sheet '{sheetName}' is empty.");

        var headers    = ReadHeaders(range);
        var searchCol  = headers.FindIndex(h =>
            string.Equals(h, searchColumn, StringComparison.OrdinalIgnoreCase));

        if (searchCol == -1)
            throw new KeyNotFoundException(
                $"Search column '{searchColumn}' not found. Available: {string.Join(", ", headers)}");

        int? matchedRow = null;
        for (int r = 2; r <= range.RowCount(); r++)
        {
            var cellValue = range.Row(r).Cell(searchCol + 1).GetString().Trim();
            if (string.Equals(cellValue, searchValue, StringComparison.OrdinalIgnoreCase))
            {
                matchedRow = r;
                break;
            }
        }

        if (matchedRow == null)
            throw new InvalidOperationException(
                $"No row found where '{searchColumn}' = '{searchValue}' in sheet '{sheetName}'.");

        foreach (var (column, value) in columnUpdates)
        {
            var colIndex = headers.FindIndex(h =>
                string.Equals(h, column, StringComparison.OrdinalIgnoreCase));

            if (colIndex == -1)
                throw new KeyNotFoundException(
                    $"Column '{column}' not found. Available: {string.Join(", ", headers)}");

            sheet.Cell(matchedRow.Value, colIndex + 1).Value = value;
        }

        workbook.Save();
    }

    // =========================================================================
    //  ADD
    // =========================================================================

    /// <summary>
    /// Appends a new row at the end of the sheet.
    /// Keys in <paramref name="rowData"/> must match the existing column headers.
    /// Columns not present in <paramref name="rowData"/> are left blank.
    /// </summary>
    public static void AddRow(
        string fileName,
        string sheetName,
        Dictionary<string, string> rowData)
        => AddRows(fileName, sheetName, new List<Dictionary<string, string>> { rowData });

    /// <summary>
    /// Appends multiple rows at the end of the sheet in one save operation.
    /// </summary>
    public static void AddRows(
        string fileName,
        string sheetName,
        List<Dictionary<string, string>> rows)
    {
        var filePath = FileUploadHelper.  GetFilePath(fileName);
        using var workbook = new XLWorkbook(filePath);
        var sheet = GetSheet(workbook, fileName, sheetName);
        var range = sheet.RangeUsed();

        List<string> headers;
        int nextRow;

        if (range == null)
        {
            // Empty sheet — build headers from first row's keys
            headers = rows[0].Keys.ToList();
            for (int c = 0; c < headers.Count; c++)
                sheet.Cell(1, c + 1).Value = headers[c];
            nextRow = 2;
        }
        else
        {
            headers = ReadHeaders(range);
            nextRow = range.RangeAddress.LastAddress.RowNumber + 1;
        }

        foreach (var rowData in rows)
        {
            for (int c = 0; c < headers.Count; c++)
            {
                var value = rowData.TryGetValue(headers[c], out var v) ? v : string.Empty;
                sheet.Cell(nextRow, c + 1).Value = value;
            }
            nextRow++;
        }

        workbook.Save();
    }

    // =========================================================================
    //  PRIVATE HELPERS
    // =========================================================================

    private static IXLWorksheet GetSheet(XLWorkbook workbook, string fileName, string sheetName)
    {
        if (!workbook.TryGetWorksheet(sheetName, out var sheet))
            throw new InvalidOperationException(
                $"Sheet '{sheetName}' not found in '{fileName}'. " +
                $"Available sheets: {string.Join(", ", workbook.Worksheets.Select(ws => ws.Name))}");
        return sheet;
    }

   

    private static List<string> ReadHeaders(IXLRange range) =>
        Enumerable.Range(1, range.Row(1).CellCount())
                  .Select(c => GetCellValue(range.Row(1).Cell(c)))
                  .ToList();
}
