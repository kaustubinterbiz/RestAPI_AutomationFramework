using EnterpriseApiAutomationFramework.Core.Builders;
using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Helpers;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Security.Patient;

/// <summary>
/// Sends Patient security requests via <see cref="RequestBuilder.BuildFlexibleDynamicRequest"/>
/// and RestClient (never UserDriver — avoids appsettings token reload).
/// </summary>
public sealed class PatientSecurityRequestExecutor
{
    private readonly RequestBuilder _requestBuilder = new();
    private readonly RestClientFactory _clientFactory = new();

    public async Task<RestResponse> ExecuteAsync(
        PatientSecurityEntry entry,
        PatientSecurityAuthState authState,
        string? bodyOverride = null,
        Dictionary<string, string>? urlSegmentOverrides = null,
        Dictionary<string, string>? queryOverrides = null)
    {
        var endpoint = EndpointConfig.GetEndpoint(entry.EndpointKey);
        var method = ParseMethod(entry.HttpMethod);

        var urlSegments = Merge(
            RequestBuilder.ResolvePartsFromConfig(entry.UrlSegmentKeys, "appsettings.json"),
            urlSegmentOverrides);

        var queryParams = Merge(
            RequestBuilder.ResolvePartsFromConfig(entry.QueryParamKeys, "appsettings.json"),
            queryOverrides);

        object? body = bodyOverride;
        if (body == null && method is Method.Post or Method.Put or Method.Patch)
            body = RequestBuilder.ResolveBodyFromConfig(entry.BodyKey, "appsettings.json");

        var flexibleOptions = new FlexibleRequestOptions
        {
            UrlSegments = urlSegments,
            QueryParams = queryParams,
            Body = body,
            BearerTokenProvided = authState.SendAuthorizationHeader && authState.BearerTokenProvided,
            BearerToken = authState.BearerToken,
            AuthorizationRequired = false
        };

        var request = _requestBuilder.BuildFlexibleDynamicRequest(endpoint, method, flexibleOptions);
        var client = _clientFactory.GetClient(ApiHost.Api);
        return await client.ExecuteAsync(request);
    }

    private static Dictionary<string, string>? Merge(
        Dictionary<string, string>? baseline,
        Dictionary<string, string>? overrides)
    {
        if (baseline == null && overrides == null)
            return null;

        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (baseline != null)
        {
            foreach (var kv in baseline)
                merged[kv.Key] = kv.Value;
        }

        if (overrides != null)
        {
            foreach (var kv in overrides)
                merged[kv.Key] = kv.Value;
        }

        return merged.Count > 0 ? merged : null;
    }

    private static Method ParseMethod(string httpMethod) =>
        httpMethod.ToUpperInvariant() switch
        {
            "GET" => Method.Get,
            "POST" => Method.Post,
            "PUT" => Method.Put,
            "PATCH" => Method.Patch,
            "DELETE" => Method.Delete,
            _ => Method.Post
        };
}
