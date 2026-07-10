using EnterpriseApiAutomationFramework.Core.Authorization;
using EnterpriseApiAutomationFramework.Core.Helpers;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.Hooks;

[Binding]
public class AuthorizationHooks
{
    private readonly ScenarioContext _scenarioContext;

    public AuthorizationHooks(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeTestRun]
    public static void EnsureAuthorizationMatrixExists()
    {
        AuthorizationConfigBootstrap.EnsureAuthorizationMatrixWorkbook();
        AuthorizationMatrixCache.Load();
        ExcelConfigBootstrap.EnsureLoginRequestWorkbook();
        ExcelConfigBootstrap.EnsureRequestEndPointWorkbook();
    }

    [BeforeScenario("@AuthMatrix")]
    public void ValidateAuthMatrixRolesExist()
    {
        if (!_scenarioContext.ScenarioInfo.Arguments.Contains("Role"))
            return;

        var role = _scenarioContext.ScenarioInfo.Arguments["Role"]?.ToString();
        if (string.IsNullOrWhiteSpace(role))
            return;

        RoleProvider.ValidateRoleExists(role);
    }

    [BeforeScenario("@TokenMatrix")]
    public void ValidateTokenMatrixConfiguration()
    {
        if (!_scenarioContext.ScenarioInfo.Arguments.Contains("ScenarioType"))
            return;

        var scenarioType = _scenarioContext.ScenarioInfo.Arguments["ScenarioType"]?.ToString();
        if (string.IsNullOrWhiteSpace(scenarioType))
            return;

        if (!AuthorizationConstants.SupportedTokenScenarios.Contains(scenarioType, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported token scenario '{scenarioType}'. Supported: {string.Join(", ", AuthorizationConstants.SupportedTokenScenarios)}");
        }
    }
}
