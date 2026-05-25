using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Configurations;
using TechTalk.SpecFlow;

namespace EnterpriseApiAutomationFramework.Hooks;

[Binding]
public class AuthenticationHooks
{
    private readonly ScenarioContext _scenarioContext;

    public AuthenticationHooks(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeScenario(Order = 0)]
    public void BeforeScenario()
    {
        TokenManager.BindScenario(_scenarioContext);
        TokenManager.ResetForNewScenario();
    }

    [AfterScenario(Order = 10000)]
    public void AfterScenario()
    {
        if (AppConfiguration.Authentication.Mode == AuthenticationMode.PerScenario)
        {
            TokenManager.ClearScenarioToken();
        }
    }
}
