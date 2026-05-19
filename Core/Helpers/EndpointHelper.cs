using System.Text.RegularExpressions;
using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

public static partial class EndpointHelper
{
    private static readonly Regex UrlSegmentPattern = UrlSegmentRegex();

    /// <summary>
    /// Resolves endpoint placeholders such as {id} using values from the active config
    /// (e.g. RequestEndPoint.json loaded via ConfigReaderNew).
    /// </summary>
    public static (string Endpoint, Dictionary<string, string> UrlSegments) ResolveUrlSegments(string endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        if (endpoint.StartsWith('$'))
        {
            endpoint = ConfigReaderNew.GetValue(endpoint[1..]);
        }

        var urlSegments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in UrlSegmentPattern.Matches(endpoint))
        {
            var key = match.Groups[1].Value;
            if (urlSegments.ContainsKey(key))
            {
                continue;
            }

            var value = ConfigReaderNew.GetValue(key);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"URL segment '{{{key}}}' was not found in the active configuration.");
            }

            urlSegments[key] = value;
        }

        return (endpoint, urlSegments);
    }

    [GeneratedRegex(@"\{(\w+)\}", RegexOptions.Compiled)]
    private static partial Regex UrlSegmentRegex();
}
