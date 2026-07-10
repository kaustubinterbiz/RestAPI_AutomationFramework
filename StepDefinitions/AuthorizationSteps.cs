using EnterpriseApiAutomationFramework.Core.Authorization;
using EnterpriseApiAutomationFramework.Core.Helpers;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.StepDefinitions;

[Binding]
public class AuthorizationSteps
{
    private readonly ScenarioContext _context;
    private readonly AuthorizationExecutor _executor;
    private readonly PermissionValidator _permissionValidator;

    public AuthorizationSteps(ScenarioContext context)
    {
        _context = context;
        _executor = new AuthorizationExecutor();
        _permissionValidator = new PermissionValidator();
    }

    [When(@"Authorization test runs for endpoint ""(.*)"" method ""(.*)"" role ""(.*)""")]
    public async Task WhenAuthorizationTestRunsForEndpointMethodRole(
        string endpointKey,
        string httpMethod,
        string role)
    {
        await _executor.ExecuteEndpointAccessAsync(_context, endpointKey, httpMethod, role);
    }

    [When(@"Authorization executes all endpoint access tests from Excel")]
    public async Task WhenAuthorizationExecutesAllEndpointAccessTestsFromExcel()
    {
        await _executor.ExecuteAllEndpointAccessRowsAsync(_context);
    }

    [When(@"Authorization executes token scenario ""(.*)"" for role ""(.*)""")]
    public async Task WhenAuthorizationExecutesTokenScenarioForRole(string scenarioType, string role)
    {
        await _executor.ExecuteTokenScenarioAsync(_context, scenarioType, role);
    }

    [When(@"Authorization executes all token validation scenarios from Excel")]
    public async Task WhenAuthorizationExecutesAllTokenValidationScenariosFromExcel()
    {
        await _executor.ExecuteAllTokenScenariosAsync(_context);
    }

    [When(@"Authorization validates all permission rules from Excel")]
    public async Task WhenAuthorizationValidatesAllPermissionRulesFromExcel()
    {
        await _permissionValidator.ValidateAllPermissionsAsync(_context);
    }

    [Then(@"Authorization status code should be (.*)")]
    public void ThenAuthorizationStatusCodeShouldBe(int expectedStatus)
    {
        var response = AuthorizationHelper.GetLastResponse(_context);
        AuthorizationHelper.ValidateStatusCode(response, expectedStatus);
    }

    [Then(@"all authorization executions should pass")]
    public void ThenAllAuthorizationExecutionsShouldPass() =>
        AuthorizationExecutionTracker.AssertAllPassed(_context);
}
