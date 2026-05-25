using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Clients;

/// <summary>
/// Resolves a feature name (from step) to <see cref="ApiHost"/> using appsettings FeatureBaseUrlMap,
/// or treats the value as a direct base URL type key (Auth / Api).
/// </summary>
public static class FeatureBaseUrlResolver
{
    public static ApiHost Resolve(string featureOrBaseUrlName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureOrBaseUrlName);

        var name = featureOrBaseUrlName.Trim();

        if (AppConfiguration.FeatureBaseUrlMap.TryGetValue(name, out var mappedTypeKey))
        {
            return ApiHostResolver.ResolveFromKey(mappedTypeKey);
        }

        if (ApiHostResolver.TryResolveFromKey(name, out var host))
        {
            return host;
        }

        throw new InvalidOperationException(
            $"No base URL mapping for feature '{name}'. " +
            $"Add it to FeatureBaseUrlMap in appsettings.json or use base url type Auth / Api.");
    }
}
