using System.Diagnostics;
using RestSharp;
using EnterpriseApiAutomationFramework.Core.Builders;
using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Core.Reporting;

namespace EnterpriseApiAutomationFramework.Core.Clients;

public class ApiClient
{
    private readonly RestClient _client;
    private readonly RequestBuilder _requestBuilder;

    public ApiClient()
    {
        _client = new RestClientFactory().GetClient();
        _requestBuilder = new RequestBuilder();
    }

    public async Task<RestResponse> GetAsync(string endpoint)
    {
        var (resolvedEndpoint, urlSegments) = EndpointHelper.ResolveUrlSegments(endpoint);
        var request = _requestBuilder.BuildRequest(
            resolvedEndpoint,
            Method.Get,
            urlSegments: urlSegments.Count > 0 ? urlSegments : null);
        return await ExecuteAndRecordAsync(request, resolvedEndpoint);
    }

    public async Task<RestResponse> PostAsync(
        string endpoint,
        object body,
        bool authorizationRequired = true)
    {
        var request = _requestBuilder.BuildRequest(
            endpoint,
            Method.Post,
            body,
            authorizationRequired: authorizationRequired);
        return await ExecuteAndRecordAsync(request, endpoint);
    }

    public async Task<RestResponse> PutAsync(string endpoint, object body)
    {
        var request = _requestBuilder.BuildRequest(endpoint, Method.Put, body);
        return await ExecuteAndRecordAsync(request, endpoint);
    }

    public async Task<RestResponse> PatchAsync(string endpoint, object body)
    {
        var request = _requestBuilder.BuildRequest(endpoint, Method.Patch, body);
        return await ExecuteAndRecordAsync(request, endpoint);
    }

    public async Task<RestResponse> DeleteAsync(string endpoint)
    {
        var request = _requestBuilder.BuildRequest(endpoint, Method.Delete);
        return await ExecuteAndRecordAsync(request, endpoint);
    }

    private async Task<RestResponse> ExecuteAndRecordAsync(RestRequest request, string endpoint)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.ExecuteAsync(request);
        stopwatch.Stop();

        ReportExecutionContext.RecordApiCall(new ApiCallRecord(
            request.Method.ToString() ?? "UNKNOWN",
            endpoint,
            (int)response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            ApiRequestPayloadHelper.Extract(request),
            response.Content));

        return response;
    }
}
