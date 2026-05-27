using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Validators;
using EnterpriseApiAutomationFramework.Drivers;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.StepDefinitions;

[Binding]
public class LoginSteps
{
    private readonly ScenarioContext _context;
    private readonly UserDriver _driver;

    public LoginSteps(ScenarioContext context)
    {
        _context = context;
        _driver = new UserDriver();
    }

    [Then(@"the access token is stored from the last login response")]
    public void ThenAccessTokenIsStoredFromLastLoginResponse()
    {
        var response = TokenContext.GetLastResponse(_context);
        ApiAuth.SaveTokenFromLoginResponse(_context, response.Content);
    }

    [When(@"User sends POST request on ""(.*)"" base url using stored access token")]
    public async Task WhenUserSendsPostOnAuthUsingStoredAccessToken(string baseUrlType)
    {
        ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        var storedToken = TokenContext.GetStoredAccessToken(_context);
        var invalidOrExpiredToken = TokenTestHelper.GetExpiredAccessToken(storedToken);
        var response = await _driver.LoginWithStoredBearerTokenAsync(invalidOrExpiredToken);
        TokenContext.SetLastResponse(_context, response);
    }

    [Then(@"login should fail")]
    public void ThenLoginShouldFail() =>
        ResponseValidator.ValidateLoginFailure(TokenContext.GetLastResponse(_context));

    [Then(@"login error message should indicate unauthorized or expired access")]
    public void ThenLoginErrorMessageShouldIndicateUnauthorizedOrExpired() =>
        ResponseValidator.ValidateUnauthorizedOrExpiredLoginMessage(TokenContext.GetLastResponse(_context));
}
