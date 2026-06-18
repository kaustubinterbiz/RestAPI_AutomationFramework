namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Writes runtime config/response values back to Excel workbooks (Phase 1: RequestEndPoint).
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
            [TestConfigDefaults.ValueColumn] = value,
            [TestConfigDefaults.UpdatedAtColumn] = DateTime.UtcNow.ToString("O")
        };

        if (!string.IsNullOrWhiteSpace(httpStatus))
            updates[TestConfigDefaults.HttpStatusColumn] = httpStatus;

        if (!string.IsNullOrWhiteSpace(responseSnippet))
            updates[TestConfigDefaults.ResponseSnippetColumn] = responseSnippet;

        UpsertKeyValue(
            TestConfigDefaults.EndpointExcelFile,
            TestConfigDefaults.EndpointResponseSheet,
            key,
            updates);
    }

    public static void UpsertKeyValue(
        string fileName,
        string sheetName,
        string key,
        IReadOnlyDictionary<string, string> columnUpdates)
    {
        var rowData = new Dictionary<string, string>(columnUpdates, StringComparer.OrdinalIgnoreCase)
        {
            [TestConfigDefaults.KeyColumn] = key
        };

        try
        {
            ExcelReader.UpdateRowWhere(
                fileName,
                sheetName,
                TestConfigDefaults.KeyColumn,
                key,
                new Dictionary<string, string>(columnUpdates, StringComparer.OrdinalIgnoreCase));
        }
        catch (InvalidOperationException)
        {
            ExcelReader.AddRow(fileName, sheetName, rowData);
        }
    }
}
