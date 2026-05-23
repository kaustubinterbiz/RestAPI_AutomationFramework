using EnterpriseApiAutomationFramework.Core.Authentication;
using TechTalk.SpecFlow;

namespace EnterpriseApiAutomationFramework.Hooks;

/// <summary>
/// Isolates bearer tokens per scenario for parallel SpecFlow execution.
/// </summary>
[Binding]
public class AuthenticationHooks
{
    [BeforeScenario(Order = 0)]
    public static void BeforeScenario()
    {
        TokenManager.ResetForNewScenario();
    }

    [AfterScenario(Order = 10000)]
    public static void AfterScenario()
    {
        TokenManager.Clear();
    }
}
