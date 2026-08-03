namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Writes runtime config/response values back to Excel workbooks.
/// </summary>
public static class ExcelConfigWriter
{
    public static void UpsertEndpointResponse(
        string key,
        string value,
        string? httpStatus = null,
        string? responseSnippet = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        ExcelConfigBootstrap.EnsureRequestEndPointWorkbook();

        var updates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [TestConfigDefaults.ValueColumn] = TruncateForExcel(value),
            [TestConfigDefaults.UpdatedAtColumn] = DateTime.UtcNow.ToString("O")
        };

        if (!string.IsNullOrWhiteSpace(httpStatus))
            updates[TestConfigDefaults.HttpStatusColumn] = httpStatus;

        if (!string.IsNullOrWhiteSpace(responseSnippet))
            updates[TestConfigDefaults.ResponseSnippetColumn] = TruncateForExcel(responseSnippet);

        UpsertBySearchColumn(
            TestConfigDefaults.EndpointExcelFile,
            TestConfigDefaults.EndpointResponseSheet,
            TestConfigDefaults.KeyColumn,
            key,
            updates);
    }

    /// <summary>
    /// Stores an API response under its endpoint key in Endpoint_Response
    /// (Key = api key, Value = primary value or body, ResponseSnippet = full body).
    /// </summary>
    public static void UpsertApiResponse(
        string apiKey,
        string responseBody,
        string? httpStatus = null,
        string? primaryValue = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        var body = responseBody ?? string.Empty;
        var value = !string.IsNullOrWhiteSpace(primaryValue) ? primaryValue : body;
        if (string.IsNullOrWhiteSpace(value))
            value = "(empty)";

        UpsertEndpointResponse(
            key: apiKey,
            value: value,
            httpStatus: httpStatus,
            responseSnippet: body);
    }

    private static string TruncateForExcel(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= TestConfigDefaults.ExcelMaxCellChars)
            return text;

        return text[..TestConfigDefaults.ExcelMaxCellChars];
    }

    public static void UpsertLoginResponse(
        string role,
        string httpStatus,
        string? tokenSnippet = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        ArgumentException.ThrowIfNullOrWhiteSpace(httpStatus);

        ExcelConfigBootstrap.EnsureLoginRequestWorkbook();

        var updates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [TestConfigDefaults.HttpStatusColumn] = httpStatus,
            [TestConfigDefaults.UpdatedAtColumn] = DateTime.UtcNow.ToString("O")
        };

        if (!string.IsNullOrWhiteSpace(tokenSnippet))
            updates[TestConfigDefaults.TokenSnippetColumn] = tokenSnippet;

        UpsertBySearchColumn(
            TestConfigDefaults.LoginExcelFile,
            TestConfigDefaults.LoginResponseSheet,
            TestConfigDefaults.RoleColumn,
            role,
            updates);
    }

    public static void UpsertBodyResponse(
        string bodyKey,
        string httpStatus,
        string? responseSnippet = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bodyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(httpStatus);

        ExcelConfigBootstrap.EnsureRequestBodyWorkbook();

        var updates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [TestConfigDefaults.HttpStatusColumn] = httpStatus,
            [TestConfigDefaults.UpdatedAtColumn] = DateTime.UtcNow.ToString("O")
        };

        if (!string.IsNullOrWhiteSpace(responseSnippet))
            updates[TestConfigDefaults.ResponseSnippetColumn] = responseSnippet;

        UpsertBySearchColumn(
            TestConfigDefaults.BodyExcelFile,
            TestConfigDefaults.BodyResponseSheet,
            TestConfigDefaults.KeyColumn,
            bodyKey,
            updates);
    }

    public static void UpsertBySearchColumn(
        string fileName,
        string sheetName,
        string searchColumn,
        string searchValue,
        IReadOnlyDictionary<string, string> columnUpdates)
    {
        var rowData = new Dictionary<string, string>(columnUpdates, StringComparer.OrdinalIgnoreCase)
        {
            [searchColumn] = searchValue
        };

        try
        {
            ExcelReader.UpdateRowWhere(
                fileName,
                sheetName,
                searchColumn,
                searchValue,
                new Dictionary<string, string>(columnUpdates, StringComparer.OrdinalIgnoreCase));
        }
        catch (InvalidOperationException)
        {
            ExcelReader.AddRow(fileName, sheetName, rowData);
        }
    }
}
