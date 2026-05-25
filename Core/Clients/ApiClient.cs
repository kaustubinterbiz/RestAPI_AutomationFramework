using System.Diagnostics;
using EnterpriseApiAutomationFramework.Core.Builders;
using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Core.Reporting;
using EnterpriseApiAutomationFramework.Models.Request;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Clients;

/// <summary>
/// HTTP facade with dynamic host switching:
/// login/token calls → Auth base URL (B2C);
/// all other verbs → Api base URL (application).
/// </summary>
public sealed class ApiClient
{
    private readonly RestClientFactory _clientFactory;
    private readonly RequestBuilder _requestBuilder;

    public ApiClient(RestClientFactory? clientFactory = null)
    {
        _clientFactory = clientFactory ?? new RestClientFactory();
        _requestBuilder = new RequestBuilder();
    }

    /// <summary>OAuth token request against the Auth (B2C) host.</summary>
    public Task<RestResponse> LoginPostAsync(string endpoint, LoginRequest credentials) =>
        LoginPostAsync(endpoint, credentials, bearerToken: null);

    public Task<RestResponse> LoginPostAsync(string endpoint, LoginRequest credentials, string? bearerToken) =>
        ExecuteAsync(
            ApiHost.Auth,
            _requestBuilder.BuildLoginRequest(endpoint, credentials, bearerToken),
            endpoint);

    public Task<RestResponse> LoginPostBearerOnlyAsync(string endpoint, string bearerToken) =>
        ExecuteAsync(
            ApiHost.Auth,
            _requestBuilder.BuildBearerOnlyPostRequest(endpoint, bearerToken),
            endpoint);

    public Task<RestResponse> GetAsync(string endpoint, ApiHost? host = null) =>
        SendAsync(endpoint, Method.Get, host: host ?? ApiHostContext.CurrentOrDefault);

    public Task<RestResponse> PostAsync(
        string endpoint,
        object body,
        bool authorizationRequired = true,
        ApiHost? host = null) =>
        SendAsync(
            endpoint,
            Method.Post,
            body,
            authorizationRequired,
            host: host ?? ApiHostContext.CurrentOrDefault);

    public Task<RestResponse> PutAsync(string endpoint, object body, ApiHost? host = null) =>
        SendAsync(endpoint, Method.Put, body, host: host ?? ApiHostContext.CurrentOrDefault);

    public Task<RestResponse> PatchAsync(string endpoint, object body, ApiHost? host = null) =>
        SendAsync(endpoint, Method.Patch, body, host: host ?? ApiHostContext.CurrentOrDefault);

    public Task<RestResponse> DeleteAsync(string endpoint, ApiHost? host = null) =>
        SendAsync(endpoint, Method.Delete, host: host ?? ApiHostContext.CurrentOrDefault);

    private async Task<RestResponse> SendAsync(
        string endpoint,
        Method method,
        object? body = null,
        bool authorizationRequired = true,
        ApiHost host = ApiHost.Api)
    {
        var (resolvedEndpoint, urlSegments) = EndpointHelper.ResolveUrlSegments(endpoint);
        var request = _requestBuilder.BuildRequest(
            resolvedEndpoint,
            method,
            body,
            urlSegments: urlSegments.Count > 0 ? urlSegments : null,
            authorizationRequired: authorizationRequired);

        return await ExecuteAsync(host, request, resolvedEndpoint);
    }

    private async Task<RestResponse> ExecuteAsync(
        ApiHost host,
        RestRequest request,
        string endpointForReport)
    {
        var client = _clientFactory.GetClient(host);
        var stopwatch = Stopwatch.StartNew();
        var response = await client.ExecuteAsync(request);
        stopwatch.Stop();

        var settings = AppConfiguration.ApiUrls;
        var baseUrl = host == ApiHost.Auth ? settings.AuthBaseUrl : settings.ApiBaseUrl;

        ReportExecutionContext.RecordApiCall(new ApiCallRecord(
            request.Method.ToString() ?? "UNKNOWN",
            $"{baseUrl.TrimEnd('/')}/{endpointForReport.TrimStart('/')}",
            (int)response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            ApiRequestPayloadHelper.Extract(request),
            response.Content));

        return response;
    }
}
