using System.Text.RegularExpressions;
using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

public static partial class EndpointHelper
{
    [GeneratedRegex(@"\{([^}]+)\}", RegexOptions.Compiled)]
    private static partial Regex UrlSegmentRegex();

    /// <summary>
    /// Strips an optional leading '$' and resolves {segment} placeholders from the active config.
    /// </summary>
    public static (string Endpoint, Dictionary<string, string> UrlSegments) ResolveUrlSegments(
        string endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        if (endpoint.StartsWith('$'))
        {
            endpoint = endpoint[1..];
        }

        var segments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in UrlSegmentRegex().Matches(endpoint))
        {
            var segmentName = match.Groups[1].Value;

            if (segments.ContainsKey(segmentName))
            {
                continue;
            }

            if (!ConfigReaderNew.IsLoaded)
            {
                throw new InvalidOperationException(
                    $"Endpoint '{endpoint}' contains '{{{segmentName}}}' but no configuration is loaded. " +
                    "Call ConfigReaderNew.LoadConfig before sending the request.");
            }

            var value = ConfigReaderNew.GetValue(segmentName);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"URL segment '{{{segmentName}}}' in endpoint '{endpoint}' has no value in the loaded configuration.");
            }

            segments[segmentName] = value;
        }

        return (endpoint, segments);
    }
}
