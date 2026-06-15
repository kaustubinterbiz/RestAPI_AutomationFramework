using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Drivers;
using EnterpriseApiAutomationFramework.Models.Response;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Services;

/// <summary>
/// Combines Excel data reading with file upload in one reusable service.
///
/// Three main operations:
///   1. UploadFromRowAsync      — read one row, upload file, return result
///   2. UploadAllRowsAsync      — loop every row, one upload per row, return all results
///   3. UploadAndTrackAsync     — upload + write Status/ResponseId back to Excel automatically
///
/// formFieldMapping (optional):
///   Key   = API form-field name  (what the API expects)
///   Value = Excel column header  (where to read the value from)
///   If null → every Excel column is sent as-is (column name = API field name).
/// </summary>
public class ExcelUploadService
{
    private readonly UserDriver _userDriver;

    public ExcelUploadService(UserDriver userDriver)
    {
        _userDriver = userDriver;
    }

    // =========================================================================
    //  1. Upload single row
    // =========================================================================

    /// <summary>
    /// Reads row <paramref name="rowIndex"/> from the sheet, builds a multipart upload request,
    /// and sends it to <paramref name="endpointKey"/>.
    /// </summary>
    public async Task<ExcelUploadResult> UploadFromRowAsync(
        string fileName,
        string sheetName,
        int rowIndex,
        string endpointKey,
        string fileParameterName = "file",
        Dictionary<string, string>? formFieldMapping = null)
    {
        var rowData   = ExcelReader.GetRow(fileName, sheetName, rowIndex);
        var formFields = BuildFormFields(rowData, formFieldMapping);

        var uploadRequest = FileUploadHelper.Build(
            endpointKey:       endpointKey,
            fileName:          fileName,
            fileParameterName: fileParameterName,
            formFields:        formFields);

        var response = await _userDriver.UploadFileAsync(uploadRequest);

        return new ExcelUploadResult
        {
            RowIndex  = rowIndex,
            RowData   = rowData,
            Response  = response
        };
    }

    // =========================================================================
    //  2. Upload all rows (one request per row)
    // =========================================================================

    /// <summary>
    /// Loops through every data row in the sheet and sends one upload request per row.
    /// Continues even if individual rows fail — check <see cref="ExcelUploadResult.IsSuccess"/>.
    /// </summary>
    public async Task<List<ExcelUploadResult>> UploadAllRowsAsync(
        string fileName,
        string sheetName,
        string endpointKey,
        string fileParameterName = "file",
        Dictionary<string, string>? formFieldMapping = null)
    {
        var allRows = ExcelReader.ReadSheet(fileName, sheetName);
        var results = new List<ExcelUploadResult>();

        for (int i = 0; i < allRows.Count; i++)
        {
            var rowIndex  = i + 1;
            var rowData   = allRows[i];
            var formFields = BuildFormFields(rowData, formFieldMapping);

            var uploadRequest = FileUploadHelper.Build(
                endpointKey:       endpointKey,
                fileName:          fileName,
                fileParameterName: fileParameterName,
                formFields:        formFields);

            var response = await _userDriver.UploadFileAsync(uploadRequest);

            results.Add(new ExcelUploadResult
            {
                RowIndex = rowIndex,
                RowData  = rowData,
                Response = response
            });
        }

        return results;
    }

    // =========================================================================
    //  3. Upload + write result back to Excel
    // =========================================================================

    /// <summary>
    /// Uploads a single row and writes the HTTP status code and (optionally) a response value
    /// back into the Excel sheet so you can track test results directly in the file.
    /// </summary>
    /// <param name="statusColumn">Excel column that receives the HTTP status code (e.g. "Status").</param>
    /// <param name="responseValueColumn">Excel column for a value extracted from the response body (e.g. "ResponseId"). Pass null to skip.</param>
    /// <param name="responseValueJsonPath">Simple top-level JSON key to extract from the response body (e.g. "id"). Pass null to skip.</param>
    public async Task<ExcelUploadResult> UploadAndTrackAsync(
        string fileName,
        string sheetName,
        int rowIndex,
        string endpointKey,
        string statusColumn           = "Status",
        string? responseValueColumn   = "ResponseId",
        string? responseValueJsonPath = "id",
        string fileParameterName      = "file",
        Dictionary<string, string>? formFieldMapping = null)
    {
        var result = await UploadFromRowAsync(
            fileName, sheetName, rowIndex, endpointKey, fileParameterName, formFieldMapping);

        var updates = new Dictionary<string, string>
        {
            { statusColumn, result.StatusCode }
        };

        // Extract one value from JSON response body when configured
        if (responseValueColumn != null && responseValueJsonPath != null
            && !string.IsNullOrWhiteSpace(result.Response.Content))
        {
            var extracted = ExtractJsonValue(result.Response.Content, responseValueJsonPath);
            if (extracted != null)
                updates[responseValueColumn] = extracted;
        }

        ExcelReader.UpdateRow(fileName, sheetName, rowIndex, updates);

        return result;
    }

    /// <summary>
    /// Uploads every row and writes status back to Excel for each row.
    /// </summary>
    public async Task<List<ExcelUploadResult>> UploadAllAndTrackAsync(
        string fileName,
        string sheetName,
        string endpointKey,
        string statusColumn           = "Status",
        string? responseValueColumn   = "ResponseId",
        string? responseValueJsonPath = "id",
        string fileParameterName      = "file",
        Dictionary<string, string>? formFieldMapping = null)
    {
        var allRows = ExcelReader.ReadSheet(fileName, sheetName);
        var results = new List<ExcelUploadResult>();

        for (int i = 0; i < allRows.Count; i++)
        {
            var result = await UploadAndTrackAsync(
                fileName, sheetName,
                rowIndex:               i + 1,
                endpointKey:            endpointKey,
                statusColumn:           statusColumn,
                responseValueColumn:    responseValueColumn,
                responseValueJsonPath:  responseValueJsonPath,
                fileParameterName:      fileParameterName,
                formFieldMapping:       formFieldMapping);

            results.Add(result);
        }

        return results;
    }

    // =========================================================================
    //  Private helpers
    // =========================================================================

    private static Dictionary<string, string> BuildFormFields(
        Dictionary<string, string> rowData,
        Dictionary<string, string>? formFieldMapping)
    {
        if (formFieldMapping == null)
            return new Dictionary<string, string>(rowData);

        // Map: Key = API field name, Value = Excel column name
        var fields = new Dictionary<string, string>();
        foreach (var (apiField, excelColumn) in formFieldMapping)
        {
            if (rowData.TryGetValue(excelColumn, out var value))
                fields[apiField] = value;
        }
        return fields;
    }

    private static string? ExtractJsonValue(string json, string key)
    {
        try
        {
            // Lightweight extraction without taking a JSON library dependency here
            var token = $"\"{key}\"";
            var idx   = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx == -1) return null;

            var colon = json.IndexOf(':', idx + token.Length);
            if (colon == -1) return null;

            var start = json.IndexOfAny(new[] { '"', '0','1','2','3','4','5','6','7','8','9','-','t','f','n' }, colon + 1);
            if (start == -1) return null;

            if (json[start] == '"')
            {
                var end = json.IndexOf('"', start + 1);
                return end == -1 ? null : json.Substring(start + 1, end - start - 1);
            }

            var endIdx = json.IndexOfAny(new[] { ',', '}', ']', '\n' }, start);
            return endIdx == -1
                ? json[start..]
                : json.Substring(start, endIdx - start).Trim();
        }
        catch
        {
            return null;
        }
    }
}
