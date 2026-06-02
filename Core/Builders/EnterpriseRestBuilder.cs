using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Interfaces;
using EnterpriseApiAutomationFramework.Core.Utilities;
using Newtonsoft.Json;
using RestSharp;
using System.Net;

namespace EnterpriseApiAutomationFramework.Core.Builders;

public class EnterpriseRestBuilder : IRestBuilder
{
    private readonly RestClient _client;
    private RestRequest _request = new();
    private string _endpoint = string.Empty;

    public EnterpriseRestBuilder()
    {
        _client = new RestClientFactory().GetClient(ApiHost.Api);
    }

    public IRestBuilder WithGet<T>(string endpoint)
    {
        _endpoint = endpoint;
        _request = CreateRequest(Method.Get);
        return this;
    }

    public IRestBuilder WithPost<T>(string endpoint)
    {
        _endpoint = endpoint;
        _request = CreateRequest(Method.Post);
        return this;
    }

    public IRestBuilder WithPut<T>(string endpoint)
    {
        _endpoint = endpoint;
        _request = CreateRequest(Method.Put);
        return this;
    }

    public IRestBuilder WithPatch<T>(string endpoint)
    {
        _endpoint = endpoint;
        _request = CreateRequest(Method.Patch);
        return this;
    }

    public IRestBuilder WithDelete<T>(string endpoint)
    {
        _endpoint = endpoint;
        _request = CreateRequest(Method.Delete);
        return this;
    }

    public IRestBuilder WithHeader(string key, string value)
    {
        _request.AddOrUpdateHeader(key, value);
        return this;
    }

    public IRestBuilder WithQueryParameter(string key, string value)
    {
        _request.AddQueryParameter(key, value);
        return this;
    }

    public IRestBuilder WithUrlSegment(string key, string value)
    {
        _request.AddUrlSegment(key, value);
        return this;
    }

    public IRestBuilder WithBody(object body)
    {
        _request.AddJsonBody(body);
        return this;
    }

    public IRestBuilder WithRequest(RestRequest request)
    {
        _request = request;
        return this;
    }

    public async Task<RestResponse> ExecuteAsync()
    {
        try
        {
            LoggerManager.LogInformation($"Executing request: {_endpoint}");
            var response = await _client.ExecuteAsync(_request);
            LoggerManager.LogInformation($"Response Status: {response.StatusCode}");
            return response;
        }
        catch (Exception ex)
        {
            LoggerManager.LogError($"Request execution failed: {ex.Message}");
            throw;
        }
    }

    public async Task<T?> ExecuteAsync<T>()
    {
        var response = await ExecuteAsync();

        if (string.IsNullOrWhiteSpace(response.Content))
        {
            return default;
        }

        return JsonConvert.DeserializeObject<T>(response.Content);
    }

    private RestRequest CreateRequest(Method method)
    {
        var request = new RestRequest(_endpoint, method);

        request.AddHeader("Content-Type", "application/json");

        if (!string.IsNullOrWhiteSpace(TokenManager.AccessToken))
        {
            request.AddOrUpdateHeader("Authorization", $"Bearer {TokenManager.AccessToken}");
        }

        return request;
    }

    private RestRequest DynamicRequestBuild(string endpoint, Method method, object? body = null,
         Dictionary<string, string>? headers = null, Dictionary<string, string>? queryParams = null, Dictionary<string, string>? urlSegments = null,
         bool authorizationRequired = true, string? explicitBearerToken = null, bool explicitBearerTokenProvided = false)
    {
        var request = new RestRequest(endpoint, method);
        if (urlSegments != null)
        {
            foreach (var segment in urlSegments)
            {
                request.AddUrlSegment(segment.Key, segment.Value);
            }
        }

        if (explicitBearerTokenProvided)
        {
            request.AddHeader("Authorization", $"Bearer {explicitBearerToken}");
        }

        else if (authorizationRequired && !string.IsNullOrEmpty(TokenManager.AccessToken))
        {
            request.AddHeader("Authorization", $"Bearer {TokenManager.AccessToken}");
        }

        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.AddHeader(header.Key, header.Value);
            }
        }

        if (queryParams != null)
        {
            foreach (var param in queryParams)
            {
                request.AddQueryParameter(param.Key, param.Value);
            }
        }

        if (body != null)
        {
            if (body is string jsonBody && !string.IsNullOrWhiteSpace(jsonBody))
            {
                request.AddStringBody(jsonBody, ContentType.Json);
            }
            else
            {
                request.AddJsonBody(body);
            }
        }
        return request;
    }
}
