using EnterpriseApiAutomationFramework.Core.Authentication;
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
        ResponseValidator.ValidateStatus(_response, "OK");

        var token = ResolveAccessToken(_response);
        token.Should().NotBeNullOrWhiteSpace("login response should contain access_token");

        _scenarioContext.Set(token, ValidAccessTokenKey);
        TokenManager.SetAccessToken(token!, persistToConfig: false);
    }

    [When(@"User applies an expired access token")]
    public void WhenUserAppliesAnExpiredAccessToken()
    {
        var validToken = _scenarioContext.Get<string>(ValidAccessTokenKey);
        _driver.ApplyExpiredAccessToken(validToken);
    }

    [When(@"User sends GET request with current token only")]
    public async Task WhenUserSendsGetRequestWithCurrentTokenOnly() =>
        await WhenUserSendsGetRequestOnBaseUrlWithCurrentTokenOnly("Api");

    [When(@"User sends GET request on ""(.*)"" base url with current token only")]
    public async Task WhenUserSendsGetRequestOnBaseUrlWithCurrentTokenOnly(string baseUrlType)
    {
        ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        _response = await _driver.GetUsersWithCurrentTokenOnly(
            "appsettings.json",
            "EndpointJson",
            "get");
    }

    [When(@"User sends GET request for feature ""(.*)"" with current token only")]
    public async Task WhenUserSendsGetRequestForFeatureWithCurrentTokenOnly(string featureName)
    {
        ApiHostStepHelper.ApplyFeatureName(featureName);
        _response = await _driver.GetUsersWithCurrentTokenOnly(
            "appsettings.json",
            "EndpointJson",
            "get");
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
        _response = await _driver.GetUsers("appsettings.json", "EndpointJson", "get");
    }

    //[When(@"User sends GET request for feature ""(.*)""")]
    //public async Task WhenUserSendsGetRequestForFeature(string featureName)
    //{
    //    ApiHostStepHelper.ApplyFeatureName(featureName);
    //    _response = await _driver.GetUsers("appsettings.json", "EndpointJson", "get");
    //}

    [Then(@"Response should indicate token error ""(.*)""")]
    public void ThenResponseShouldIndicateTokenError(string expectedFragment) =>
        ResponseValidator.ValidateExpiredOrInvalidTokenError(_response!, expectedFragment);

    [Then(@"the API status code should be (.*)")]
    public void ThenTheApiStatusCodeShouldBe(int statusCode) =>
        ResponseValidator.ValidateStatusCode(_response!, statusCode);

    private static string? ResolveAccessToken(RestResponse? response)
    {
        if (!string.IsNullOrWhiteSpace(TokenManager.AccessToken))
        {
            return TokenManager.AccessToken;
        }

        return LoginResponseParser.TryGetAccessToken(response?.Content);
    }
}
