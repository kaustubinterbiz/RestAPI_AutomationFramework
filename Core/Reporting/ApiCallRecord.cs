namespace EnterpriseApiAutomationFramework.Core.Reporting;

public sealed class ApiCallRecord
{
    public ApiCallRecord(
        string method,
        string endpoint,
        int statusCode,
        long elapsedMilliseconds,
        string? requestPayload,
        string? responseBody)
    {
        Method = method;
        Endpoint = endpoint;
        StatusCode = statusCode;
        ElapsedMilliseconds = elapsedMilliseconds;
        RequestPayload = requestPayload;
        ResponseBody = responseBody;
    }

    public string Method { get; }
    public string Endpoint { get; }
    public int StatusCode { get; }
    public long ElapsedMilliseconds { get; }
    public string? RequestPayload { get; }
    public string? ResponseBody { get; }

    public bool ExceedsThreshold(long expectedMilliseconds) =>
        ElapsedMilliseconds > expectedMilliseconds;
}
