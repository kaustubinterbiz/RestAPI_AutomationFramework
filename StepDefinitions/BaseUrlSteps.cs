using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Hooks;
using TechTalk.SpecFlow;

namespace EnterpriseApiAutomationFramework.StepDefinitions;

[Binding]
public class BaseUrlSteps
{
    [Given(@"the base url type is ""(.*)""")]
    public void GivenTheBaseUrlTypeIs(string baseUrlType) =>
        ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);

    [Given(@"feature ""(.*)"" uses base url type ""(.*)""")]
    public void GivenFeatureUsesBaseUrlType(string featureName, string baseUrlType)
    {
        ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
    }
}
