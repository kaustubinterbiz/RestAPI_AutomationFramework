using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Core.Security.Patient;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Security.PatientSearch;

public sealed class PatientSearchSecurityRequestExecutor
{
    private readonly RestClientFactory _clientFactory = new();

    public async Task<RestResponse> ExecutePostAsync(
        ApiHost host,
        string bodyJson,
        PatientSearchSecurityAuthContext auth,
        string pathSegment = "1")
    {
        PatientSearchSecurityBootstrap.EnsureInfrastructure();

        var useDynamicPath = !string.Equals(pathSegment, "1", StringComparison.Ordinal);
        var endpointKey = useDynamicPath
            ? PatientSearchSecurityConstants.EndpointKeyDynamicPath
            : PatientSearchSecurityConstants.EndpointKey;

        var endpointTemplate = EndpointConfig.GetEndpoint(endpointKey);
        string resolvedEndpoint;
        Dictionary<string, string> urlSegments;

        if (useDynamicPath)
        {
            resolvedEndpoint = endpointTemplate;
            urlSegments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["pathSegment"] = pathSegment
            };
        }
        else
        {
            (resolvedEndpoint, urlSegments) = EndpointHelper.ResolveEndpoint(endpointTemplate);
        }

        var request = new RestRequest(resolvedEndpoint, Method.Post);
        foreach (var segment in urlSegments)
            request.AddUrlSegment(segment.Key, segment.Value);

        request.AddHeader("Accept", "application/json");
        request.AddHeader("Content-Type", "application/json");

        if (host == ApiHost.Apim)
        {
            request.AddHeader("Origin", "https://test.rovicare.com");
            request.AddHeader("Referer", "https://test.rovicare.com/");
        }

        var cacheId = EndpointRequestHelper.GetCachedValue("CacheId");
        if (!string.IsNullOrWhiteSpace(cacheId))
            request.AddHeader("CacheId", cacheId);

        ApplyAuthorization(request, auth);

        request.AddStringBody(bodyJson, ContentType.Json);
        var client = _clientFactory.GetClient(host);
        return await client.ExecuteAsync(request);
    }

    private static void ApplyAuthorization(RestRequest request, PatientSearchSecurityAuthContext auth)
    {
        if (!auth.SendAuthorizationHeader)
            return;

        if (auth.UseRawAuthorizationHeader && !string.IsNullOrWhiteSpace(auth.AuthorizationHeaderValue))
        {
            request.AddHeader("Authorization", auth.AuthorizationHeaderValue);
            return;
        }

        if (auth.BearerTokenProvided)
        {
            request.AddHeader("Authorization", $"Bearer {auth.BearerToken ?? string.Empty}");
            return;
        }

        if (!string.IsNullOrWhiteSpace(TokenManager.AccessToken))
            request.AddHeader("Authorization", $"Bearer {TokenManager.AccessToken}");
    }
}
