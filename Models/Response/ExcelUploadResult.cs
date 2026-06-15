using RestSharp;

namespace EnterpriseApiAutomationFramework.Models.Response;

public class ExcelUploadResult
{
    public int RowIndex { get; init; }

    public Dictionary<string, string> RowData { get; init; } = new();

    public RestResponse Response { get; init; } = null!;

    public bool IsSuccess => (int)Response.StatusCode is >= 200 and < 300;

    public string StatusCode => ((int)Response.StatusCode).ToString();
}
