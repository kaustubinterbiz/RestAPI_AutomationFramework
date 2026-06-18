using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Builders;
using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Core.Reporting;
using EnterpriseApiAutomationFramework.Models.Request;
using RestSharp;
using System.Diagnostics;
using System.Net;

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

    /// <summary>OAuth token request against the Auth (B2C) host.</summary>  ----> Read credential by Json
    public Task<RestResponse> LoginPostAsync(string endpoint, LoginRequest credentials) =>
        LoginPostAsync(endpoint, credentials, bearerToken: null);

    public Task<RestResponse> LoginPostAsync(string endpoint, LoginRequest credentials, string? bearerToken) =>
        ExecuteAsync(ApiHost.Auth, _requestBuilder.BuildLoginRequest(endpoint, credentials, bearerToken), endpoint);

    public Task<RestResponse> LoginPostBearerOnlyAsync(string endpoint, string bearerToken) =>
        ExecuteAsync(ApiHost.Auth, _requestBuilder.BuildBearerOnlyPostRequest(endpoint, bearerToken), endpoint);

    public Task<RestResponse> GetAsync(string endpoint, ApiHost? host = null) =>
                              GetAsync(endpoint, options: null, host: host ?? ApiHostContext.CurrentOrDefault);

    /// <summary>
    /// GET with optional body and bearer token.
    /// Skips body/token when not set via <see cref="ApiGetRequestOptions"/>.
    /// Throws only when body/token was explicitly provided but null or empty.
    /// </summary>
    public Task<RestResponse> GetAsync(string endpoint, ApiGetRequestOptions? options, 
        ApiHost? host = null) => SendGetAsync(endpoint, options, host ?? ApiHostContext.CurrentOrDefault);

    public Task<RestResponse> PostAsync(string endpoint, object body, bool authorizationRequired = true,
        ApiHost? host = null) =>SendAsync(endpoint, Method.Post, body, authorizationRequired, host: host ?? ApiHostContext.CurrentOrDefault);

    public Task<RestResponse> PutAsync(string endpoint, object body, ApiHost? host = null) =>
        SendAsync(endpoint, Method.Put, body, host: host ?? ApiHostContext.CurrentOrDefault);

    public Task<RestResponse> PatchAsync(string endpoint, object body, ApiHost? host = null) =>
        SendAsync(endpoint, Method.Patch, body, host: host ?? ApiHostContext.CurrentOrDefault);

    public Task<RestResponse> DeleteAsync(string endpoint, ApiHost? host = null) =>
        SendAsync(endpoint, Method.Delete, host: host ?? ApiHostContext.CurrentOrDefault);
 

    private async Task<RestResponse> SendGetAsync(string endpoint, ApiGetRequestOptions? options, ApiHost host)
    {
        options ??= ApiGetRequestOptions.Create();
        options.ValidateProvidedValues();

        var (resolvedEndpoint, urlSegments) = EndpointHelper.ResolveEndpoint(endpoint);

        object? body = options.BodyProvided ? options.Body : null;
        var useCachedToken = options.UseCachedTokenWhenTokenNotProvided && !options.BearerTokenProvided;

        var bearerTokenProvided = options.BearerTokenProvided;
        var bearerToken = options.BearerToken;

        if (useCachedToken && !bearerTokenProvided)
        {
            if (!TokenManager.HasToken)
            {
                TokenManager.InitializeFromConfig();
            }

            if (TokenManager.HasToken)
            {
                bearerToken = TokenManager.AccessToken;
                bearerTokenProvided = true;
            }
        }

        var request = _requestBuilder.BuildRequest(
            resolvedEndpoint,
            Method.Get,
            body,
            urlSegments: urlSegments.Count > 0 ? urlSegments : null,
            authorizationRequired: useCachedToken && !bearerTokenProvided,
            explicitBearerToken: bearerToken,
            explicitBearerTokenProvided: bearerTokenProvided);

        return await ExecuteAsync(host, request, resolvedEndpoint);
    }

    

    public async Task<RestResponse> SendRequestAsync(string endpoint,string? filename, string? key, string? targetValue, string? headerkey, 
        string? quaryParamKey,
        Method method, ApiGetRequestOptions? options, ApiHost host)
    {
        string resolvedEndpoint;
        Dictionary<string, string> urlSegments = new Dictionary<string, string>();
        options ??= ApiGetRequestOptions.Create();
        options.ValidateProvidedValues();
        ConfigReaderNew.LoadConfig(filename ?? "appsettings.json");
        if (key != null)
        {
            var values = new Dictionary<string, string>{
                             { key, ConfigReaderNew.GetValue(key) }
                         };
            resolvedEndpoint = EndpointHelper.ResolveUrlPlaceholders(
                              endpoint,
                              values,
                              targetKeys: targetValue,
                              valueKeys: key);
        }
        else
        {
            (resolvedEndpoint, urlSegments) =
                EndpointHelper.ResolveEndpoint(endpoint);
        }


        Dictionary<string, string>? headerss = null;
        if (headerkey != null)
        {
            var headerValue = ConfigReaderNew.GetValue(headerkey);
            
            if (!string.IsNullOrEmpty(headerkey) && headerValue != null)
            {
                headerss = new Dictionary<string, string> { [headerkey] = headerValue };
            }
        }

        Dictionary<string, string>? queryParams = null;
        if (quaryParamKey != null)
        {
            var paramValue = ConfigReaderNew.GetValue(quaryParamKey);
            
            if (!string.IsNullOrEmpty(quaryParamKey) && paramValue != null)
            {
                queryParams = new Dictionary<string, string> { [quaryParamKey] = paramValue };
            }
        }

        object? body = null;
        if (Method.Post == method || Method.Put == method || Method.Patch == method)
        {
            body = options.BodyProvided ? options.Body : null;
        }

        var useCachedToken = options.UseCachedTokenWhenTokenNotProvided && !options.BearerTokenProvided;

        var bearerTokenProvided = options.BearerTokenProvided;
        var bearerToken = options.BearerToken;

        if (useCachedToken && !bearerTokenProvided)
        {
            if (!TokenManager.HasToken)
            {
                TokenManager.InitializeFromConfig();
            }

            if (TokenManager.HasToken)
            {
                bearerToken = TokenManager.AccessToken;
                bearerTokenProvided = true;
            }
        }

        var request = _requestBuilder.DynamicRequestBuild(
            resolvedEndpoint,
            method,
            body,
            headers: headerss,
            queryParams: null,
            urlSegments: urlSegments.Count > 0 ? urlSegments : null,
            authorizationRequired: useCachedToken && !bearerTokenProvided,
            explicitBearerToken: bearerToken,
            explicitBearerTokenProvided: bearerTokenProvided);

        return await ExecuteAsync(host, request, resolvedEndpoint);
    }

    /// <summary>
    /// Sends a request with optional dynamic headers, query params, url segments, and body.
    /// Each part is resolved from config only when its comma-separated key string is provided; otherwise skipped.
    /// </summary>
    //public async Task<RestResponse> SendFlexibleRequestAsync(
    //    string endpoint,
    //    string? configFile,
    //    string? urlPlaceholderKeys,
    //    string? targetValue,
    //    string? headerKeys,
    //    string? queryParamKeys,
    //    string? urlSegmentKeys,
    //    Method method,
    //    ApiGetRequestOptions? options,
    //    ApiHost host)
    //{
    //    options ??= ApiGetRequestOptions.Create();
    //    options.ValidateProvidedValues();

    //    var config = configFile ?? "appsettings.json";
    //    string resolvedEndpoint = endpoint;
    //    Dictionary<string, string>? urlSegments = null;

    //    if (!string.IsNullOrWhiteSpace(urlSegmentKeys))
    //    {
    //        urlSegments = RequestBuilder.ResolvePartsFromConfig(urlSegmentKeys, config);
    //        if (!string.IsNullOrWhiteSpace(targetValue) && urlSegments != null)
    //        {
    //            var value = urlSegments.Values.FirstOrDefault()
    //            ?? EndpointRequestHelper.GetCachedValue(targetValue);
    //            urlSegments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    //            {
    //                [targetValue] = value
    //            };
    //            resolvedEndpoint = endpoint;   
    //        }
    //            var configSegments = RequestBuilder.ResolvePartsFromConfig(urlSegmentKeys, config);
    //        if (configSegments != null)
    //        {
    //            urlSegments ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    //            foreach (var segment in configSegments)
    //            {
    //                urlSegments[segment.Key] = segment.Value;
    //            }
    //        }
    //    }

    //    Dictionary<string, string>? headers = null;
    //    if (!string.IsNullOrWhiteSpace(headerKeys))
    //    {
    //        headers = RequestBuilder.ResolvePartsFromConfig(headerKeys, config);
    //    }

    //    Dictionary<string, string>? queryParams = null;
    //    if (!string.IsNullOrWhiteSpace(queryParamKeys))
    //    {
    //        queryParams = RequestBuilder.ResolvePartsFromConfig(queryParamKeys, config);
    //    }

    //    object? body = null;
    //    if (method is Method.Post or Method.Put or Method.Patch)
    //    {
    //        body = options.BodyProvided ? options.Body : null;
    //    }

    //    var useCachedToken = options.UseCachedTokenWhenTokenNotProvided && !options.BearerTokenProvided;
    //    var bearerTokenProvided = options.BearerTokenProvided;
    //    var bearerToken = options.BearerToken;

    //    if (useCachedToken && !bearerTokenProvided)
    //    {
    //        if (!TokenManager.HasToken)
    //        {
    //            TokenManager.InitializeFromConfig();
    //        }

    //        if (TokenManager.HasToken)
    //        {
    //            bearerToken = TokenManager.AccessToken;
    //            bearerTokenProvided = true;
    //        }
    //    }

    //    var flexibleOptions = new FlexibleRequestOptions
    //    {
    //        Headers = headers,
    //        QueryParams = queryParams,
    //        UrlSegments = urlSegments,
    //        Body = body,
    //        AuthorizationRequired = useCachedToken && !bearerTokenProvided,
    //        BearerToken = bearerToken,
    //        BearerTokenProvided = bearerTokenProvided
    //    };

    //    if (!string.IsNullOrWhiteSpace(urlPlaceholderKeys))
    //    {
    //        var placeholderValues = RequestBuilder.ResolvePartsFromConfig(urlPlaceholderKeys, config);
    //        resolvedEndpoint = placeholderValues != null
    //            ? EndpointHelper.ResolveUrlPlaceholders(endpoint, placeholderValues, targetValue)
    //            : endpoint;
    //    }
    //    else
    //    {
    //        var (resolved, endpointSegments) = EndpointHelper.ResolveEndpoint(endpoint);
    //        resolvedEndpoint = resolved;
    //        if (endpointSegments.Count > 0)
    //        {
    //            urlSegments = endpointSegments;
    //        }
    //    }

    //    var request = _requestBuilder.BuildFlexibleDynamicRequest(resolvedEndpoint, method, flexibleOptions);
    //    return await ExecuteAsync(host, request, resolvedEndpoint);
    //}
    private static string GetEndpointBasePath(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return endpoint;
        var queryIndex = endpoint.IndexOf('?', StringComparison.Ordinal);
        return queryIndex >= 0 ? endpoint[..queryIndex] : endpoint;
    }

    public async Task<RestResponse> SendFlexibleRequestAsync(
    string endpoint,
    string? configFile,
    string? urlPlaceholderKeys,
    string? targetValue,
    string? headerKeys,
    string? queryParamKeys,
    string? urlSegmentKeys,
    Method method,
    ApiGetRequestOptions? options,
    ApiHost host,
    string? bodyKey = null)
    {
        options ??= ApiGetRequestOptions.Create();
        options.ValidateProvidedValues();

        var config = configFile ?? "appsettings.json";
        string resolvedEndpoint;
        Dictionary<string, string>? urlSegments = null;
        Dictionary<string, string>? queryParams = null;

        if (!string.IsNullOrWhiteSpace(urlSegmentKeys))
        {
            // MODE 1: URL segments only
            urlSegments = RequestBuilder.ResolvePartsFromConfig(urlSegmentKeys, config);

            if (!string.IsNullOrWhiteSpace(targetValue) && urlSegments is { Count: > 0 })
            {
                var segmentKey = targetValue.Trim();
                string segmentValue;

                if (urlSegments.TryGetValue(segmentKey, out var found))
                {
                    segmentValue = found;
                }
                else
                {
                    segmentValue = urlSegments.Values.First();
                }

                urlSegments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [segmentKey] = segmentValue
                };
            }

            resolvedEndpoint = endpoint;
        }
        else if (!string.IsNullOrWhiteSpace(queryParamKeys))
        {
            // MODE 2: Sirf {} values replace — EmailID/BusinessUnitID keys same
            var placeholderValues = RequestBuilder.ResolvePartsFromConfig(queryParamKeys, config);

            if (endpoint.Contains('{', StringComparison.Ordinal))
            {
                resolvedEndpoint = EndpointHelper.ResolveEndpointPlaceholders(endpoint, placeholderValues);
                queryParams = null;
            }
            else
            {
                // Plain path (bina ?) → normal AddQueryParameter
                queryParams = placeholderValues;
                resolvedEndpoint = endpoint;
            }
        }
        else if (!string.IsNullOrWhiteSpace(urlPlaceholderKeys))
        {
            var placeholderValues = RequestBuilder.ResolvePartsFromConfig(urlPlaceholderKeys, config);
            resolvedEndpoint = placeholderValues != null
                ? EndpointHelper.ResolveUrlPlaceholders(
                    endpoint,
                    placeholderValues,
                    targetKeys: targetValue,
                    valueKeys: urlPlaceholderKeys)   
                : endpoint;
        }
        else
        {
            // MODE 3b: Auto — saare {} cache/email se
            var (resolved, endpointSegments) = EndpointHelper.ResolveEndpoint(endpoint);
            resolvedEndpoint = resolved;
            if (endpointSegments.Count > 0)
            {
                urlSegments = endpointSegments;
            }
        }

        Dictionary<string, string>? headers = null;
        if (!string.IsNullOrWhiteSpace(headerKeys))
        {
            headers = RequestBuilder.ResolvePartsFromConfig(headerKeys, config);
        }

        object? body = null;
        if (method is Method.Post or Method.Put or Method.Patch)
        {
            var bodyJson = RequestBuilder.ResolveBodyFromConfig(bodyKey, config);
            if (bodyJson != null)
            {
                body = bodyJson;
            }
            else if (options.BodyProvided)
            {
                body = options.Body;
            }
        }

        var useCachedToken = options.UseCachedTokenWhenTokenNotProvided && !options.BearerTokenProvided;
        var bearerTokenProvided = options.BearerTokenProvided;
        var bearerToken = options.BearerToken;

        if (useCachedToken && !bearerTokenProvided)
        {
            if (!TokenManager.HasToken)
            {
                TokenManager.InitializeFromConfig();
            }

            if (TokenManager.HasToken)
            {
                bearerToken = TokenManager.AccessToken;
                bearerTokenProvided = true;
            }
        }

        var flexibleOptions = new FlexibleRequestOptions
        {
            Headers = headers,
            QueryParams = queryParams,
            UrlSegments = urlSegments,
            Body = body,
            AuthorizationRequired = useCachedToken && !bearerTokenProvided,
            BearerToken = bearerToken,
            BearerTokenProvided = bearerTokenProvided
        };

        var request = _requestBuilder.BuildFlexibleDynamicRequest(resolvedEndpoint, method, flexibleOptions);
        return await ExecuteAsync(host, request, resolvedEndpoint);
    }

    /// <summary>
    /// Multipart file upload to any endpoint.
    /// Attaches bearer token from <see cref="TokenManager"/> when available.
    /// </summary>
    public async Task<RestResponse> UploadFileAsync(FileUploadRequest uploadRequest, ApiHost host = ApiHost.Api)
    {
        var request = new RestRequest(uploadRequest.Endpoint, Method.Post);

        // Bearer token from TokenManager (same pattern as the rest of the client)
        if (TokenManager.HasToken)
            request.AddHeader("Authorization", $"Bearer {TokenManager.AccessToken}");

        if (uploadRequest.Headers != null)
            foreach (var header in uploadRequest.Headers)
                request.AddHeader(header.Key, header.Value);

        if (uploadRequest.FormFields != null)
            foreach (var field in uploadRequest.FormFields)
                request.AddParameter(field.Key, field.Value);

        request.AddFile(uploadRequest.FileParameterName, uploadRequest.FilePath);

        return await ExecuteAsync(host, request, uploadRequest.Endpoint);
    }

    private async Task<RestResponse> SendAsync(
        string endpoint,
        Method method,
        object? body = null,
        bool authorizationRequired = true,
        ApiHost host = ApiHost.Api)
    {
        var (resolvedEndpoint, urlSegments) = EndpointHelper.ResolveEndpoint(endpoint);
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
