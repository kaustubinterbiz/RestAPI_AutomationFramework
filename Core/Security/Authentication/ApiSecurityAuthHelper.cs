using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Authorization;
using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Drivers;
using EnterpriseApiAutomationFramework.StepDefinitions;
using Reqnroll;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Security.Authentication;

/// <summary>
/// Executes one Authentication (JWT-gate) security row against an Api endpoint.
/// <para>
/// Important: does NOT call <see cref="UserDriver.SendFlexibleRequestAsync"/> for mutated tokens,
/// because that path reloads <c>access_token</c> from appsettings and would overwrite the mutation.
/// </para>
/// </summary>
public sealed class ApiSecurityAuthHelper
{
    private readonly ApiClient _apiClient;
    private readonly UserDriver _driver;
    private readonly RestClientFactory _restClientFactory;

    /// <summary>Cached baseline tokens per role for one ExcelLoop run (avoids 21 logins).</summary>
    private readonly Dictionary<string, string> _baselineTokensByRole =
        new(StringComparer.OrdinalIgnoreCase);

    public ApiSecurityAuthHelper(ApiClient? apiClient = null, UserDriver? driver = null)
    {
        _apiClient = apiClient ?? new ApiClient();
        _driver = driver ?? new UserDriver(_apiClient);
        _restClientFactory = new RestClientFactory();
    }

    /// <summary>
    /// Obtain (or reuse) a valid access token for the role. Call once per role in an ExcelLoop.
    /// </summary>
    public async Task<string> EnsureBaselineTokenAsync(ScenarioContext context, string role)
    {
        if (_baselineTokensByRole.TryGetValue(role, out var cached) && !string.IsNullOrWhiteSpace(cached))
            return cached;

        RoleProvider.ValidateRoleExists(role);
        SharedTokenProvider.InvalidateAllCaches();
        TokenManager.ClearScenarioToken();

        ApiHostStepHelper.ApplyBaseUrlType("Auth");
        var loginResponse = await _driver.LoginAsync(role);
        if (!loginResponse.IsSuccessful)
        {
            throw new InvalidOperationException(
                $"Baseline login failed for role '{role}'. " +
                $"Status={(int)loginResponse.StatusCode}. Body={Truncate(loginResponse.Content, 200)}");
        }

        ApiAuth.SaveTokenFromLoginResponse(context, loginResponse.Content);
        var token = TokenManager.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                $"Baseline login for '{role}' did not produce an access_token.");
        }

        _baselineTokensByRole[role] = token;
        return token;
    }

    /// <summary>
    /// Apply JWT mutation → call Api endpoint → return response.
    /// Pass <paramref name="baselineToken"/> from <see cref="EnsureBaselineTokenAsync"/> when needed.
    /// </summary>
    public async Task<RestResponse> ExecuteAsync(
        ScenarioContext context,
        ApiSecurityAuthEntry entry,
        string? baselineToken = null)
    {
        ValidateScenarioType(entry.ScenarioType);

        SharedTokenProvider.InvalidateAllCaches();
        TokenManager.ClearScenarioToken();

        string? validToken = baselineToken;
        if (ScenarioNeedsBaselineLogin(entry.ScenarioType) && string.IsNullOrWhiteSpace(validToken))
            validToken = await EnsureBaselineTokenAsync(context, entry.Role);

        ApplyMutation(entry.ScenarioType, validToken);
        ApplyHostForEndpoint(entry.EndpointKey);

        return await SendApiRequestAsync(entry);
    }

    private static bool ScenarioNeedsBaselineLogin(string scenarioType) =>
        scenarioType.ToUpperInvariant() switch
        {
            "NOAUTHHEADER" => false,
            "EMPTYBEARER" => false,
            _ => true
        };

    private static void ValidateScenarioType(string scenarioType)
    {
        if (!ApiSecurityAuthConstants.SupportedScenarioTypes.Contains(
                scenarioType,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Unsupported ScenarioType '{scenarioType}'. Supported: " +
                string.Join(", ", ApiSecurityAuthConstants.SupportedScenarioTypes));
        }
    }

    private static void ApplyMutation(string scenarioType, string? validToken)
    {
        switch (scenarioType.ToUpperInvariant())
        {
            case "NOAUTHHEADER":
            case "EMPTYBEARER":
                TokenManager.ClearScenarioToken();
                SharedTokenProvider.InvalidateAllCaches();
                break;

            case "INVALIDTOKEN":
                SharedTokenProvider.ApplyExpiredTokenForTesting(TokenTestHelper.GetInvalidAccessToken());
                break;

            case "MALFORMEDTOKEN":
                SharedTokenProvider.ApplyExpiredTokenForTesting(TokenTestHelper.GetMalformedAccessToken());
                break;

            case "EXPIREDTOKEN":
                // QA API does not reject structurally expired JWTs (see ExpiredAccessToken.json).
                // Use the file-based / fallback expired token so Session returns 401 like InvalidToken cases.
                SharedTokenProvider.ApplyExpiredTokenForTesting(
                    TokenTestHelper.GetExpiredAccessToken(validToken));
                break;

            // Post-login only: start from real JWT → mutate claim → keep signature → expect 401 on API.
            case "TAMPEREDTOKEN":
                if (string.IsNullOrWhiteSpace(validToken))
                {
                    throw new InvalidOperationException(
                        "TamperedToken requires a baseline valid JWT to modify payload claims.");
                }

                SharedTokenProvider.ApplyExpiredTokenForTesting(
                    TokenTestHelper.GetTamperedAccessToken(validToken));
                break;

            case "MISSINGBEARER":
                if (string.IsNullOrWhiteSpace(validToken))
                {
                    throw new InvalidOperationException(
                        "MissingBearer requires a baseline valid token.");
                }

                TokenManager.SetAccessToken(validToken, persistToConfig: false);
                break;

            default:
                throw new ArgumentException($"Unsupported ScenarioType '{scenarioType}'.");
        }
    }

    private static void ApplyHostForEndpoint(string endpointKey)
    {
        if (string.Equals(endpointKey, "get", StringComparison.OrdinalIgnoreCase))
            ApiHostStepHelper.ApplyFeatureName(ApiSecurityAuthConstants.SessionFeatureName);
        else
            ApiHostStepHelper.ApplyBaseUrlType("Api");
    }

    private async Task<RestResponse> SendApiRequestAsync(ApiSecurityAuthEntry entry)
    {
        var method = ParseMethod(entry.HttpMethod);
        var endpoint = EndpointConfig.GetEndpoint(entry.EndpointKey);
        var scenario = entry.ScenarioType.ToUpperInvariant();

        return scenario switch
        {
            "NOAUTHHEADER" =>
                await ExecuteRawAsync(endpoint, method, entry, authorizationHeader: null),
            "EMPTYBEARER" =>
                await ExecuteRawAsync(endpoint, method, entry, authorizationHeader: "Bearer "),
            "MISSINGBEARER" =>
                await ExecuteRawAsync(endpoint, method, entry, authorizationHeader: TokenManager.AccessToken),
            _ =>
                await ExecuteRawAsync(
                    endpoint,
                    method,
                    entry,
                    authorizationHeader: string.IsNullOrWhiteSpace(TokenManager.AccessToken)
                        ? null
                        : $"Bearer {TokenManager.AccessToken}")
        };
    }

    /// <summary>
    /// Builds RestRequest directly so mutated Authorization headers are never overwritten
    /// by appsettings token reload.
    /// </summary>
    private async Task<RestResponse> ExecuteRawAsync(
        string endpoint,
        Method method,
        ApiSecurityAuthEntry entry,
        string? authorizationHeader)
    {
        var (resolvedEndpoint, urlSegments) = EndpointHelper.ResolveEndpoint(endpoint);
        var request = new RestRequest(resolvedEndpoint, method);

        foreach (var segment in urlSegments)
            request.AddUrlSegment(segment.Key, segment.Value);

        AddConfiguredQueryParams(request, entry.QueryParamKeys);

        if (authorizationHeader != null)
            request.AddHeader("Authorization", authorizationHeader);

        if (!string.IsNullOrWhiteSpace(entry.HeaderKeys))
        {
            foreach (var key in entry.HeaderKeys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var value = EndpointRequestHelper.GetCachedValue(key);
                if (!string.IsNullOrWhiteSpace(value))
                    request.AddHeader(key, value);
            }
        }

        var client = _restClientFactory.GetClient(ApiHost.Api);
        return await client.ExecuteAsync(request);
    }

    private static void AddConfiguredQueryParams(RestRequest request, string? queryParamKeys)
    {
        if (string.IsNullOrWhiteSpace(queryParamKeys))
            return;

        foreach (var key in queryParamKeys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var value = EndpointRequestHelper.GetCachedValue(key);
            if (!string.IsNullOrWhiteSpace(value))
                request.AddQueryParameter(key, value);
        }
    }

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

    private static string Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Length <= max
                ? value
                : value[..max] + "...";
}
