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

    [Then(@"session info from the last response is stored in appsettings")]
    public void ThenSessionInfoFromLastResponseIsStoredInAppSettings()
    {
        var response = TokenContext.GetLastResponse(_context);
        StoreInfo.SaveSessionInfoFromResponse(response.Content);
    }

    [Then("Store the info for AddMultipleMemberByExcel")]
    public void ThenStoreTheInfoForAddMultipleMemberByExcel()
    {
        var response = TokenContext.GetLastResponse(_context);
        StoreInfo.SaveAddMultiMemberByExcelResponseToExcel(response.Content);
        StoreInfo.SaveAddMultiMemberByExcelFromResponse(response.Content);
    }

    [Then(@"Store the AddMultipleMemberByExcel response in excel file ""(.*)"" sheet ""(.*)""")]
    public void ThenStoreAddMultipleMemberResponseInExcel(string fileName, string sheetName)
    {
        var response = TokenContext.GetLastResponse(_context);
        StoreInfo.SaveAddMultiMemberByExcelResponseToExcel(response.Content, fileName, sheetName);
        StoreInfo.SaveAddMultiMemberByExcelFromResponse(response.Content);
    }

    [Then(@"Store the AddMultipleMemberByExcel response in excel file ""(.*)"" sheet ""(.*)"" columns ""(.*)""")]
    public void ThenStoreAddMultipleMemberResponseInExcelWithColumns(
        string fileName,
        string sheetName,
        string columns)
    {
        var response = TokenContext.GetLastResponse(_context);
        StoreInfo.SaveAddMultiMemberByExcelResponseToExcel(
            response.Content,
            fileName,
            sheetName,
            ParseExcelColumns(columns));
        StoreInfo.SaveAddMultiMemberByExcelFromResponse(response.Content);
    }

    private static List<string>? ParseExcelColumns(string columns)
    {
        if (string.IsNullOrWhiteSpace(columns) || string.Equals(columns.Trim(), "-", StringComparison.Ordinal))
            return null;

        return columns
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }


    [Then("existingUser info from the last response is stored in appsettings")]
    public void ThenExistingUserInfoFromTheLastResponseIsStoredInAppsettings()
    {
        var response = TokenContext.GetLastResponse(_context);
        StoreInfo.SaveExistingUserFromResponse(response.Content);
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
