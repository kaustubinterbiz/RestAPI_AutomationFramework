using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Core.Validators;
using EnterpriseApiAutomationFramework.Drivers;
using EnterpriseApiAutomationFramework.StepDefinitions;
using Reqnroll;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Authorization;

/// <summary>
/// Reusable authorization helper: token generation, request execution, and response validation.
/// Wraps existing AuthService, TokenManager, and ApiClient without modifying them.
/// </summary>
public sealed class AuthorizationHelper
{
    private readonly ApiClient _apiClient;
    private readonly UserDriver _driver;

    public AuthorizationHelper(ApiClient? apiClient = null, UserDriver? driver = null)
    {
        _apiClient = apiClient ?? new ApiClient();
        _driver = driver ?? new UserDriver(_apiClient);
    }

    public async Task<string> GenerateTokenAsync(ScenarioContext context, string role)
    {
        if (RoleProvider.IsGuestRole(role))
        {
            TokenManager.ClearScenarioToken();
            return string.Empty;
        }

        RoleProvider.ValidateRoleExists(role);
        SharedTokenProvider.InvalidateAllCaches();

        ApiHostStepHelper.ApplyBaseUrlType("Auth");
        var loginResponse = await _driver.LoginAsync(role);
        ResponseValidator.ValidateStatus(loginResponse, "OK");
        ApiAuth.SaveTokenFromLoginResponse(context, loginResponse.Content);

        return TokenManager.AccessToken;
    }

    public async Task PrepareSessionAsync(ScenarioContext context, string role)
    {
        await GenerateTokenAsync(context, role);

        ApiHostStepHelper.ApplyFeatureName(AuthorizationConstants.SessionFeatureName);
        var sessionResponse = await _driver.GetAsync();
        StoreInfo.SaveSessionInfoFromResponse(sessionResponse.Content);
    }

    public async Task<RestResponse> ExecuteEndpointAccessAsync(
        ScenarioContext context,
        AuthorizationMatrixEntry entry)
    {
        if (entry.RequiresSession && !RoleProvider.IsGuestRole(entry.Role))
            await PrepareSessionAsync(context, entry.Role);
        else if (!RoleProvider.IsGuestRole(entry.Role))
            await GenerateTokenAsync(context, entry.Role);
        else
            TokenManager.ClearScenarioToken();

        var host = ResolveHostForEndpoint(entry.EndpointKey);

        if (string.Equals(entry.EndpointKey, "get", StringComparison.OrdinalIgnoreCase))
            ApiHostStepHelper.ApplyFeatureName(AuthorizationConstants.SessionFeatureName);
        else
            ApiHostStepHelper.ApplyBaseUrlType(host == ApiHost.Auth ? "Auth" : "Api");

        var method = ParseMethod(entry.HttpMethod);
        var options = BuildOptionsForRole(entry.Role, headerMode: "Bearer");

        if (method == Method.Get && string.IsNullOrWhiteSpace(entry.HeaderKeys))
        {
            var endpoint = EndpointConfig.GetEndpoint(entry.EndpointKey);
            return await _apiClient.GetAsync(endpoint, options, host);
        }

        return await _driver.SendFlexibleRequestAsync(
            configFile: "appsettings.json",
            urlPlaceholderKeys: entry.UrlPlaceholderKeys,
            targetValue: entry.TargetValue,
            headerKeys: entry.HeaderKeys,
            queryParamKeys: entry.QueryParamKeys,
            urlSegmentKeys: null,
            method: method,
            endpointKey: entry.EndpointKey,
            options: options,
            host: host,
            bodyKey: null);
    }

    public async Task<RestResponse> ExecuteTokenScenarioAsync(
        ScenarioContext context,
        AuthorizationTokenScenarioEntry entry)
    {
        return entry.ScenarioType.ToUpperInvariant() switch
        {
            "VALIDTOKEN" => await ExecuteValidTokenScenarioAsync(context, entry),
            "EMPTYTOKEN" => await ExecuteEmptyTokenScenarioAsync(entry),
            "MISSINGBEARER" => await ExecuteMissingBearerScenarioAsync(context, entry),
            "INVALIDTOKEN" or "EXPIREDTOKEN" or "TAMPEREDTOKEN" =>
                await ExecuteNegativeBearerOnAuthAsync(context, entry),
            _ => throw new ArgumentException($"Unsupported token scenario type '{entry.ScenarioType}'.")
        };
    }

    private async Task<RestResponse> ExecuteValidTokenScenarioAsync(
        ScenarioContext context,
        AuthorizationTokenScenarioEntry entry)
    {
        var role = entry.Role is "-" or "" ? "SuperAdmin" : entry.Role;
        ApiHostStepHelper.ApplyBaseUrlType("Auth");
        var loginResponse = await _driver.LoginAsync(role);
        ApiAuth.SaveTokenFromLoginResponse(context, loginResponse.Content);
        return loginResponse;
    }

    private async Task<RestResponse> ExecuteEmptyTokenScenarioAsync(AuthorizationTokenScenarioEntry entry)
    {
        ApiHostStepHelper.ApplyBaseUrlType(entry.BaseUrl);
        if (string.Equals(entry.BaseUrl, "Auth", StringComparison.OrdinalIgnoreCase))
        {
            var loginEndpoint = EndpointConfig.GetEndpoint("post");
            return await SendPostWithoutAuthorizationAsync(loginEndpoint, ApiHost.Auth);
        }

        if (string.Equals(entry.EndpointKey, "get", StringComparison.OrdinalIgnoreCase))
            ApiHostStepHelper.ApplyFeatureName(AuthorizationConstants.SessionFeatureName);

        var endpoint = EndpointConfig.GetEndpoint(entry.EndpointKey);
        return await SendGetWithoutAuthorizationAsync(endpoint, ApiHost.Api);
    }

    private async Task<RestResponse> ExecuteMissingBearerScenarioAsync(
        ScenarioContext context,
        AuthorizationTokenScenarioEntry entry)
    {
        var role = entry.Role is "-" or "" ? "SuperAdmin" : entry.Role;
        await GenerateTokenAsync(context, role);

        if (string.Equals(entry.BaseUrl, "Auth", StringComparison.OrdinalIgnoreCase))
        {
            var loginEndpoint = EndpointConfig.GetEndpoint("post");
            return await SendPostWithAuthorizationHeaderAsync(
                loginEndpoint,
                TokenManager.AccessToken,
                ApiHost.Auth);
        }

        if (string.Equals(entry.EndpointKey, "get", StringComparison.OrdinalIgnoreCase))
            ApiHostStepHelper.ApplyFeatureName(AuthorizationConstants.SessionFeatureName);

        var endpoint = EndpointConfig.GetEndpoint(entry.EndpointKey);
        return await SendGetWithAuthorizationHeaderAsync(endpoint, TokenManager.AccessToken, ApiHost.Api);
    }

    private async Task<RestResponse> ExecuteNegativeBearerOnAuthAsync(
        ScenarioContext context,
        AuthorizationTokenScenarioEntry entry)
    {
        var role = entry.Role is "-" or "" ? "SuperAdmin" : entry.Role;
        await GenerateTokenAsync(context, role);
        var validToken = TokenManager.AccessToken;

        ApplyTokenScenario(entry.ScenarioType, validToken, entry.HeaderMode);

        ApiHostStepHelper.ApplyBaseUrlType("Auth");
        return await _driver.LoginWithStoredBearerTokenAsync(TokenManager.AccessToken);
    }

    public async Task<RestResponse> ExecutePermissionCheckAsync(
        ScenarioContext context,
        AuthorizationPermissionEntry entry)
    {
        if (entry.RequiresSession)
            await PrepareSessionAsync(context, entry.Role);
        else
            await GenerateTokenAsync(context, entry.Role);

        ApiHostStepHelper.ApplyBaseUrlType("Api");
        var method = ParseMethod(entry.HttpMethod);
        var options = ApiGetRequestOptions.Create();

        return await _driver.SendFlexibleRequestAsync(
            configFile: "appsettings.json",
            urlPlaceholderKeys: null,
            targetValue: null,
            headerKeys: entry.HeaderKeys,
            queryParamKeys: entry.EndpointKey.Contains("ExistingUser", StringComparison.OrdinalIgnoreCase)
                ? "EmailId"
                : null,
            urlSegmentKeys: null,
            method: method,
            endpointKey: entry.EndpointKey,
            options: options,
            host: ApiHost.Api,
            bodyKey: entry.BodyKey);
    }

    public static void ValidateStatusCode(RestResponse response, int expectedStatusCode) =>
        ResponseValidator.ValidateStatusCode(response, expectedStatusCode);

    public static void ValidateForbidden(RestResponse response) =>
        ResponseValidator.ValidateStatusCode(response, AuthorizationConstants.StatusForbidden);

    public static void ValidateUnauthorized(RestResponse response) =>
        ResponseValidator.ValidateStatusCode(response, AuthorizationConstants.StatusUnauthorized);

    public static void StoreLastResponse(ScenarioContext context, RestResponse response, int expectedStatus)
    {
        context.Set(response, AuthorizationConstants.LastResponseKey);
        context.Set(expectedStatus, AuthorizationConstants.LastExpectedStatusKey);
        TokenContext.SetLastResponse(context, response);
    }

    public static RestResponse GetLastResponse(ScenarioContext context) =>
        context.Get<RestResponse>(AuthorizationConstants.LastResponseKey);

    private static void ApplyTokenScenario(string scenarioType, string? validToken, string headerMode)
    {
        switch (scenarioType.ToUpperInvariant())
        {
            case "VALIDTOKEN":
                return;

            case "INVALIDTOKEN":
            case "TAMPEREDTOKEN":
                SharedTokenProvider.ApplyExpiredTokenForTesting(
                    TokenTestHelper.GetExpiredAccessToken(validToken));
                return;

            case "EXPIREDTOKEN":
                SharedTokenProvider.ApplyExpiredTokenForTesting(
                    TokenTestHelper.GetExpiredAccessToken(validToken));
                return;

            case "EMPTYTOKEN":
                TokenManager.ClearScenarioToken();
                return;

            case "MISSINGBEARER":
                if (!string.IsNullOrWhiteSpace(validToken))
                    TokenManager.SetAccessToken(validToken, persistToConfig: false);
                return;

            default:
                throw new ArgumentException($"Unsupported token scenario type '{scenarioType}'.");
        }
    }

    private static ApiGetRequestOptions BuildOptionsForRole(string role, string headerMode)
    {
        var options = ApiGetRequestOptions.Create();
        options.UseCachedTokenWhenTokenNotProvided = !RoleProvider.IsGuestRole(role);
        return options;
    }

    private async Task<RestResponse> SendGetWithAuthorizationHeaderAsync(
        string endpoint,
        string authorizationHeaderValue,
        ApiHost host)
    {
        var (resolvedEndpoint, urlSegments) = EndpointHelper.ResolveEndpoint(endpoint);
        var request = new RestRequest(resolvedEndpoint, Method.Get);
        foreach (var segment in urlSegments)
            request.AddUrlSegment(segment.Key, segment.Value);

        if (!string.IsNullOrWhiteSpace(authorizationHeaderValue))
            request.AddHeader("Authorization", authorizationHeaderValue);

        var client = new RestClientFactory().GetClient(host);
        return await client.ExecuteAsync(request);
    }

    private async Task<RestResponse> SendGetWithoutAuthorizationAsync(string endpoint, ApiHost host)
    {
        var (resolvedEndpoint, urlSegments) = EndpointHelper.ResolveEndpoint(endpoint);
        var request = new RestRequest(resolvedEndpoint, Method.Get);
        foreach (var segment in urlSegments)
            request.AddUrlSegment(segment.Key, segment.Value);

        var client = new RestClientFactory().GetClient(host);
        return await client.ExecuteAsync(request);
    }

    private async Task<RestResponse> SendPostWithoutAuthorizationAsync(string endpoint, ApiHost host)
    {
        var (resolvedEndpoint, urlSegments) = EndpointHelper.ResolveEndpoint(endpoint);
        var request = new RestRequest(resolvedEndpoint, Method.Post);
        foreach (var segment in urlSegments)
            request.AddUrlSegment(segment.Key, segment.Value);

        var client = new RestClientFactory().GetClient(host);
        return await client.ExecuteAsync(request);
    }

    private async Task<RestResponse> SendPostWithAuthorizationHeaderAsync(
        string endpoint,
        string authorizationHeaderValue,
        ApiHost host)
    {
        var (resolvedEndpoint, urlSegments) = EndpointHelper.ResolveEndpoint(endpoint);
        var request = new RestRequest(resolvedEndpoint, Method.Post);
        foreach (var segment in urlSegments)
            request.AddUrlSegment(segment.Key, segment.Value);

        if (!string.IsNullOrWhiteSpace(authorizationHeaderValue))
            request.AddHeader("Authorization", authorizationHeaderValue);

        var client = new RestClientFactory().GetClient(host);
        return await client.ExecuteAsync(request);
    }

    private static ApiHost ResolveHostForEndpoint(string endpointKey) =>
        string.Equals(endpointKey, "post", StringComparison.OrdinalIgnoreCase)
            ? ApiHost.Auth
            : ApiHost.Api;

    private static Method ParseMethod(string httpMethod) =>
        httpMethod.ToUpperInvariant() switch
        {
            "GET" => Method.Get,
            "POST" => Method.Post,
            "PUT" => Method.Put,
            "PATCH" => Method.Patch,
            "DELETE" => Method.Delete,
            _ => throw new ArgumentException($"Unsupported HTTP method '{httpMethod}'.")
        };
}
