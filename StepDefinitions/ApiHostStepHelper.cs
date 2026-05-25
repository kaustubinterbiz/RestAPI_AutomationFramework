using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Hooks;

namespace EnterpriseApiAutomationFramework.StepDefinitions;

internal static class ApiHostStepHelper
{
    public static ApiHost ApplyBaseUrlType(string baseUrlType)
    {
        var host = ApiHostResolver.ResolveFromKey(baseUrlType);
        ApiHostHooks.SetHost(host);
        return host;
    }

    public static ApiHost ApplyFeatureName(string featureName)
    {
        var host = FeatureBaseUrlResolver.Resolve(featureName);
        ApiHostHooks.SetHost(host);
        return host;
    }
}
