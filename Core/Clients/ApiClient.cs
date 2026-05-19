using RestSharp;
using EnterpriseApiAutomationFramework.Core.Builders;
using EnterpriseApiAutomationFramework.Core.Helpers;

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
        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse> PostAsync(string endpoint, object body)
    {
        var request = _requestBuilder.BuildRequest(endpoint, Method.Post, body);
        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse> PutAsync(string endpoint, object body)
    {
        var request = _requestBuilder.BuildRequest(endpoint, Method.Put, body);
        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse> PatchAsync(string endpoint, object body)
    {
        var request = _requestBuilder.BuildRequest(endpoint, Method.Patch, body);
        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse> DeleteAsync(string endpoint)
    {
        var request = _requestBuilder.BuildRequest(endpoint, Method.Delete);
        return await _client.ExecuteAsync(request);
    }
}