using System.Security.Cryptography;
using System.Text;
using ClosedXML.Excel;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Read and write Excel files stored in TestData/UploadFiles/.
/// Pass only the filename and sheet name — folder path is auto-resolved.
/// First row of every sheet is always treated as the header row.
/// </summary>
public static class ExcelReader
{
    // ---------------------------------------------------------------------
    //  Concurrency-safe workbook access
    // ---------------------------------------------------------------------
    // Two independent test scenarios (even in *separate* test-host processes
    // under parallel execution) used to collide on the same .xlsx file — one
    // reading while another saved — producing
    // "The process cannot access the file ... because it is being used by
    // another process".
    //
    // The fix has three layers so the file is never held open longer than a
    // few milliseconds and access is fully serialized machine-wide:
    //   1. A *named* Mutex keyed on the file path serializes every open/save
    //      across ALL threads AND processes (an in-process lock alone cannot
    //      coordinate separate test-host processes).
    //   2. The file is slurped into memory (FileStream with
    //      FileShare.ReadWrite) and the handle is closed immediately, so a
    //      read never fails just because someone else has the file open
    //      (e.g. viewing it in Excel).
    //   3. Writes are atomic: the workbook is rebuilt in memory and swapped
    //      in via a temp file + File.Move, so the real file is never left
    //      half-written or locked. A short retry rides out transient
    //      OS/antivirus locks.

    private const int MaxAttempts = 20;
    private static readonly TimeSpan MutexTimeout = TimeSpan.FromSeconds(90);

    /// <summary>Opens the workbook at <paramref name="filePath"/>, runs <paramref name="body"/>,
    /// and optionally persists the changes — serialized cross-process with retry.</summary>
    internal static T WithWorkbook<T>(string filePath, Func<XLWorkbook, T> body, bool save)
    {
        var fullPath = Path.GetFullPath(filePath);
        return WithFileMutex(fullPath, () => RunWithRetry(() => save
            ? MutateInMemory(fullPath, body)
            : ReadInMemory(fullPath, body)));
    }

    internal static void WithWorkbook(string filePath, Action<XLWorkbook> body, bool save) =>
        WithWorkbook(filePath, wb => { body(wb); return true; }, save);

    private static T ReadInMemory<T>(string fullPath, Func<XLWorkbook, T> body)
    {
        using var input = new MemoryStream(ReadAllBytesShared(fullPath), writable: false);
        using var workbook = new XLWorkbook(input);
        return body(workbook);
    }

    private static T MutateInMemory<T>(string fullPath, Func<XLWorkbook, T> body)
    {
        T result;
        byte[] outBytes;
        using (var input = new MemoryStream(ReadAllBytesShared(fullPath), writable: false))
        using (var workbook = new XLWorkbook(input))
        {
            result = body(workbook);
            using var output = new MemoryStream();
            workbook.SaveAs(output);
            outBytes = output.ToArray();
        }

        WriteAllBytesAtomic(fullPath, outBytes);
        return result;
    }

    /// <summary>Reads the whole file into memory, tolerating other open handles, then closes it.</summary>
    private static byte[] ReadAllBytesShared(string fullPath)
    {
        using var fs = new FileStream(
            fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var ms = new MemoryStream();
        fs.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes bytes to <paramref name="fullPath"/> without holding the handle open.
    /// Order: in-place overwrite → File.WriteAllBytes → temp+Copy → temp+replace via rename.
    /// Avoids relying on File.Move(overwrite) first — that often throws
    /// UnauthorizedAccessException under VS debug when the destination is briefly locked.
    /// </summary>
    private static void WriteAllBytesAtomic(string fullPath, byte[] data)
    {
        ClearReadOnlyAttribute(fullPath);

        if (TryWriteInPlace(fullPath, data))
            return;

        try
        {
            File.WriteAllBytes(fullPath, data);
            return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // fall through
        }

        var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var backupPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".bak";
        try
        {
            File.WriteAllBytes(tempPath, data);

            try
            {
                File.Copy(tempPath, fullPath, overwrite: true);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Replace by renaming the destination aside, then moving temp into place.
                ClearReadOnlyAttribute(fullPath);
                File.Move(fullPath, backupPath);
                try
                {
                    File.Move(tempPath, fullPath);
                    tempPath = null;
                }
                catch
                {
                    // Roll back backup if we failed to place the new file.
                    try { if (!File.Exists(fullPath) && File.Exists(backupPath)) File.Move(backupPath, fullPath); }
                    catch { /* best effort */ }
                    throw;
                }
            }
        }
        finally
        {
            if (tempPath != null)
                TryDelete(tempPath);
            TryDelete(backupPath);
        }
    }

    private static bool TryWriteInPlace(string fullPath, byte[] data)
    {
        try
        {
            using var fs = new FileStream(
                fullPath,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.ReadWrite);
            fs.SetLength(0);
            fs.Write(data, 0, data.Length);
            fs.Flush(true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void ClearReadOnlyAttribute(string path)
    {
        try
        {
            if (!File.Exists(path))
                return;

            var attrs = File.GetAttributes(path);
            if ((attrs & FileAttributes.ReadOnly) != 0)
                File.SetAttributes(path, attrs & ~FileAttributes.ReadOnly);
        }
        catch
        {
            // best effort — retry loop will surface a real failure
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best effort — a leftover temp file must never fail a test */ }
    }

    /// <summary>Serializes access to a single file across all threads and processes on the machine.</summary>
    private static T WithFileMutex<T>(string fullPath, Func<T> action)
    {
        using var mutex = new Mutex(initiallyOwned: false, MutexNameFor(fullPath));
        var owned = false;
        try
        {
            try
            {
                owned = mutex.WaitOne(MutexTimeout);
            }
            catch (AbandonedMutexException)
            {
                owned = true; // prior owner crashed; we now own it
            }

            if (!owned)
            {
                throw new TimeoutException(
                    $"Timed out after {MutexTimeout.TotalSeconds:0}s waiting for exclusive access to '{fullPath}'.");
            }

            return action();
        }
        finally
        {
            if (owned)
            {
                try { mutex.ReleaseMutex(); }
                catch (ApplicationException) { /* not owned on this thread */ }
            }
        }
    }

    private static string MutexNameFor(string fullPath)
    {
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(fullPath.ToLowerInvariant())));
        // No namespace prefix => "Local\" (session-wide), shared by every test-host process.
        return "ExcelReader_" + hash;
    }

    private static T RunWithRetry<T>(Func<T> action)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
                when (attempt < MaxAttempts && IsTransientFileAccessError(ex))
            {
                Thread.Sleep(Math.Min(100 * attempt, 800));
            }
        }
    }

    /// <summary>
    /// True for sharing/access denials that clear after a short wait
    /// (VS debug testhost, antivirus, indexer). Not for missing paths.
    /// </summary>
    private static bool IsTransientFileAccessError(Exception ex) =>
        ex is UnauthorizedAccessException
        || (ex is IOException
            && ex is not FileNotFoundException
            && ex is not DirectoryNotFoundException);

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
        return WithWorkbook(filePath, workbook =>
        {
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
        }, save: false);
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
        return WithWorkbook(filePath,
            workbook => workbook.Worksheets.Select(ws => ws.Name).ToList(),
            save: false);
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
        WithWorkbook(filePath, workbook =>
        {
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
        }, save: true);
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
        WithWorkbook(filePath, workbook =>
        {
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
        }, save: true);
    }

    /// <summary>
    /// Replaces all data rows on a sheet (keeps row 1 headers).
    /// Uses existing header row when present; adds any missing columns from <paramref name="columnOrder"/> or row data.
    /// Creates the worksheet when it does not exist.
    /// </summary>
    public static void ReplaceSheetData(
        string fileName,
        string sheetName,
        IReadOnlyList<Dictionary<string, string>> rowsData,
        IReadOnlyList<string>? columnOrder = null)
    {
        var filePath = FileUploadHelper.GetFilePath(fileName);
        WithWorkbook(filePath, workbook =>
        {
            if (!workbook.TryGetWorksheet(sheetName, out var sheet))
                sheet = workbook.AddWorksheet(sheetName);

            var range = sheet.RangeUsed();
            List<string> headers;

            if (range == null || IsHeaderRowEmpty(sheet))
            {
                headers = columnOrder?.ToList()
                    ?? (rowsData.Count > 0
                        ? rowsData[0].Keys.ToList()
                        : throw new InvalidOperationException(
                            $"Sheet '{sheetName}' has no headers and no row data was provided."));

                for (int c = 0; c < headers.Count; c++)
                    sheet.Cell(1, c + 1).Value = headers[c];
            }
            else
            {
                headers = ReadHeaders(range);
                EnsureHeaders(sheet, headers, columnOrder ?? headers);
                EnsureHeaders(sheet, headers, rowsData.SelectMany(r => r.Keys));
            }

            ClearDataRows(sheet, headers.Count);

            for (int r = 0; r < rowsData.Count; r++)
            {
                for (int c = 0; c < headers.Count; c++)
                {
                    var value = rowsData[r].TryGetValue(headers[c], out var v) ? v : string.Empty;
                    sheet.Cell(r + 2, c + 1).Value = value;
                }
            }
        }, save: true);
    }

    private static bool IsHeaderRowEmpty(IXLWorksheet sheet)
    {
        var firstRow = sheet.Row(1);
        return firstRow.IsEmpty() || firstRow.CellsUsed().All(c => string.IsNullOrWhiteSpace(GetCellValue(c)));
    }

    private static void EnsureHeaders(IXLWorksheet sheet, List<string> headers, IEnumerable<string> columnNames)
    {
        foreach (var column in columnNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (headers.Any(h => string.Equals(h, column, StringComparison.OrdinalIgnoreCase)))
                continue;

            headers.Add(column);
            sheet.Cell(1, headers.Count).Value = column;
        }
    }

    private static void ClearDataRows(IXLWorksheet sheet, int columnCount)
    {
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        if (lastRow <= 1)
            return;

        sheet.Range(2, 1, lastRow, Math.Max(columnCount, 1)).Clear(XLClearOptions.Contents);
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
        var filePath = FileUploadHelper.GetFilePath(fileName);
        WithWorkbook(filePath, workbook =>
        {
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
        }, save: true);
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
