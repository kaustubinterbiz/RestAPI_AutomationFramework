using System.Text.RegularExpressions;
using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

public static partial class EndpointHelper
{
    private static readonly Regex UrlSegmentPattern = UrlSegmentRegex();

    /// <summary>
    /// Resolves {id}, {access_token}, etc. from active config / TokenManager.
    /// Path placeholders use RestSharp url segments; query placeholders are inlined.
    /// </summary>
    public static (string Endpoint, Dictionary<string, string> UrlSegments) ResolveEndpoint(string endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        if (endpoint.StartsWith('$'))
        {
            endpoint = ConfigReaderNew.GetValue(endpoint[1..]);
        }

        if (!endpoint.Contains('{'))
        {
            return (endpoint, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        if (endpoint.Contains('?', StringComparison.Ordinal))
        {
            return (ReplacePlaceholders(endpoint), new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        var urlSegments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in UrlSegmentPattern.Matches(endpoint))
        {
            var key = match.Groups[1].Value;
            if (urlSegments.ContainsKey(key))
            {
                continue;
            }

            urlSegments[key] = EndpointRequestHelper.GetCachedValue(key);
        }

        return (endpoint, urlSegments);
    }

    public static (string Endpoint, Dictionary<string, string> UrlSegments) ResolveUrlSegments(string endpoint) =>
        ResolveEndpoint(endpoint);

    private static string ReplacePlaceholders(string endpoint)
    {
        var result = endpoint;
        foreach (Match match in UrlSegmentPattern.Matches(endpoint))
        {
            var key = match.Groups[1].Value;
            var value = EndpointRequestHelper.GetCachedValue(key);
            result = result.Replace(match.Value, Uri.EscapeDataString(value), StringComparison.Ordinal);
        }

        return result;
    }

    [GeneratedRegex(@"\{(\w+)\}", RegexOptions.Compiled)]
    private static partial Regex UrlSegmentRegex();
}
