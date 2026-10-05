using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Security.Reporting;
using EnterpriseApiAutomationFramework.Core.Validators;
using EnterpriseApiAutomationFramework.Drivers;
using FluentAssertions;
using RestSharp;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.StepDefinitions;

[Binding]
public class TokenSteps
{
    private const string ValidAccessTokenKey = "ValidAccessToken";
    private const string TamperedAccessTokenKey = "TamperedAccessToken";

    private readonly ScenarioContext _scenarioContext;
    private readonly UserDriver _driver;
    private RestResponse? _response;

    public TokenSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _driver = new UserDriver();
    }

    [Given(@"User has a valid access token")]
    public async Task GivenUserHasAValidAccessToken() =>
        await GivenUserHasAValidAccessTokenOnBaseUrl("Auth");

    [Given(@"User has a valid access token on ""(.*)"" base url")]
    public async Task GivenUserHasAValidAccessTokenOnBaseUrl(string baseUrlType)
    {
        ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        SharedTokenProvider.InvalidateAllCaches();
        _response = await _driver.LoginAsync();
        CaptureResponse();
        ResponseValidator.ValidateStatus(_response, "OK");

        var token = ResolveAccessToken(_response);
        token.Should().NotBeNullOrWhiteSpace("login response should contain access_token");

        _scenarioContext.Set(token, ValidAccessTokenKey);
        TokenManager.SetAccessToken(token!, persistToConfig: false);
    }

    [When(@"User applies an expired access token")]
    public void WhenUserAppliesAnExpiredAccessToken() =>
        _driver.ApplyExpiredAccessToken(ResolveBaselineToken());

    /// <summary>
    /// Captured valid JWT → tamper into unauthorized bearer → store for next GET (expect 401).
    /// </summary>
    [When(@"User applies a tampered access token")]
    public void WhenUserAppliesATamperedAccessToken()
    {
        var validToken = ResolveBaselineToken();
        var tampered = TokenTestHelper.GetTamperedAccessToken(validToken);
        _scenarioContext.Set(tampered, TamperedAccessTokenKey);
        _driver.ApplyTamperedAccessToken(validToken);
    }

    [When(@"User applies a wrong issuer audience access token")]
    public void WhenUserAppliesAWrongIssuerAudienceAccessToken() =>
        _driver.ApplyWrongIssuerAudienceAccessToken(ResolveBaselineToken());

    [When(@"User applies a missing claim access token")]
    public void WhenUserAppliesAMissingClaimAccessToken() =>
        _driver.ApplyMissingClaimAccessToken(ResolveBaselineToken());

    [When(@"User sends GET request with current token only")]
    public async Task WhenUserSendsGetRequestWithCurrentTokenOnly() =>
        await WhenUserSendsGetRequestOnBaseUrlWithCurrentTokenOnly("Api");

    [When(@"User sends GET request on ""(.*)"" base url with current token only")]
    public async Task WhenUserSendsGetRequestOnBaseUrlWithCurrentTokenOnly(string baseUrlType)
    {
        ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);

        // After tamper: always send the unauthorized bearer explicitly (never valid cached token).
        if (_scenarioContext.TryGetValue(TamperedAccessTokenKey, out string tampered)
            && !string.IsNullOrWhiteSpace(tampered))
        {
            _response = await _driver.GetWithBearerTokenAsync(tampered);
            CaptureResponse();
            return;
        }

        _response = await _driver.GetWithCurrentTokenAsync();
        CaptureResponse();
    }

    [When(@"User sends GET request for feature ""(.*)"" with current token only")]
    public async Task WhenUserSendsGetRequestForFeatureWithCurrentTokenOnly(string featureName)
    {
        ApiHostStepHelper.ApplyFeatureName(featureName);
        _response = await _driver.GetWithCurrentTokenAsync();
        CaptureResponse();
    }

    [When(@"User refreshes the access token")]
    public async Task WhenUserRefreshesTheAccessToken() =>
        await WhenUserRefreshesTheAccessTokenOnBaseUrl("Auth");

    [When(@"User refreshes the access token on ""(.*)"" base url")]
    public async Task WhenUserRefreshesTheAccessTokenOnBaseUrl(string baseUrlType)
    {
        ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        SharedTokenProvider.InvalidateAllCaches();
        _response = await _driver.RefreshAccessTokenAsync();
        CaptureResponse();
        ResponseValidator.ValidateStatus(_response, "OK");

        var token = ResolveAccessToken(_response);
        token.Should().NotBeNullOrWhiteSpace("refresh should return access_token");
        _scenarioContext.Set(token, ValidAccessTokenKey);
        TokenManager.SetAccessToken(token!, persistToConfig: false);
    }

    [When(@"User sends GET request after token refresh")]
    public async Task WhenUserSendsGetRequestAfterTokenRefresh()
    {
        ApiHostStepHelper.ApplyFeatureName("Access Token Refresh");
        _response = await _driver.GetAsync();
        CaptureResponse();
    }

    [Then(@"Response should indicate token error ""(.*)""")]
    public void ThenResponseShouldIndicateTokenError(string expectedFragment) =>
        ResponseValidator.ValidateExpiredOrInvalidTokenError(_response!, expectedFragment);

    [Then(@"the API status code should be (.*)")]
    public void ThenTheApiStatusCodeShouldBe(int statusCode)
    {
        CaptureResponse();
        _scenarioContext.Set(statusCode, SecurityReportingConstants.LastExpectedStatusKey);
        ResponseValidator.ValidateStatusCode(_response!, statusCode);
    }

    private void CaptureResponse()
    {
        if (_response != null)
            TokenContext.SetLastResponse(_scenarioContext, _response);
    }

    private static string? ResolveAccessToken(RestResponse? response)
    {
        if (!string.IsNullOrWhiteSpace(TokenManager.AccessToken))
        {
            return TokenManager.AccessToken;
        }

        return LoginResponseParser.TryGetAccessToken(response?.Content);
    }

    private string ResolveBaselineToken()
    {
        if (_scenarioContext.TryGetValue(ValidAccessTokenKey, out string valid)
            && !string.IsNullOrWhiteSpace(valid))
        {
            return valid;
        }

        if (_scenarioContext.TryGetValue(TokenContext.StoredAccessTokenKey, out string stored)
            && !string.IsNullOrWhiteSpace(stored))
        {
            return stored;
        }

        if (!string.IsNullOrWhiteSpace(TokenManager.AccessToken))
            return TokenManager.AccessToken;

        throw new InvalidOperationException(
            "A valid access token is required. Run login and store token first.");
    }
}
