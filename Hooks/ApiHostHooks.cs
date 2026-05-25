using EnterpriseApiAutomationFramework.Core.Clients;
using TechTalk.SpecFlow;

namespace EnterpriseApiAutomationFramework.Hooks;

[Binding]
public class ApiHostHooks
{
    private readonly ScenarioContext _scenarioContext;
    private readonly FeatureContext _featureContext;

    public ApiHostHooks(ScenarioContext scenarioContext, FeatureContext featureContext)
    {
        _scenarioContext = scenarioContext;
        _featureContext = featureContext;
    }

    [BeforeScenario(Order = 1)]
    public void ApplyBaseUrlTypeFromFeatureTags()
    {
        var scenarioTags = _scenarioContext.ScenarioInfo.Tags;
        var featureTags = _featureContext.FeatureInfo.Tags;

        var host =
            ApiHostResolver.ResolveFromTags(scenarioTags)
            ?? ApiHostResolver.ResolveFromTags(featureTags);

        if (host.HasValue)
        {
            SetHost(host.Value);
        }
    }

    [AfterScenario(Order = 9999)]
    public void ClearBaseUrlType()
    {
        ApiHostContext.Clear();
    }

    internal static void SetHost(ApiHost host)
    {
        ApiHostContext.Set(host);
    }
}
