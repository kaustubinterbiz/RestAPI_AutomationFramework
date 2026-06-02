using System.Text.Json;
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

    public static Dictionary<string, string>? BuildQueryParams(
    string? queryParam,
    Dictionary<string, string> configValues)
    {
        if (string.IsNullOrWhiteSpace(queryParam))
            return null;

        var dict = new Dictionary<string, string>();

        var items = queryParam.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var item in items)
        {
            var part = item.Trim();

            // -----------------------------
            // CASE 1: key=value format
            // -----------------------------
            if (part.Contains('='))
            {
                var kv = part.Split('=', 2);

                var key = kv[0].Trim();
                var value = kv[1].Trim();

                dict[key] = ResolveValue(value, configValues);
            }
            // -----------------------------
            // CASE 2: only key format
            // -----------------------------
            else
            {
                var key = part;

                dict[key] =
                    configValues.TryGetValue(key, out var val)
                        ? val
                        : ConfigReaderNew.GetValue(key);
            }
        }

        return dict;
    }

    public static string ResolvePlaceholdersFromJsonFiles( string endPoint, params string[] jsonFiles)
    {
        var allValues = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var file in jsonFiles)
        {
            var json = File.ReadAllText(file);

            var values =
                JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (values == null)
            {
                continue;
            }

            foreach (var kvp in values)
            {
                allValues[kvp.Key] = kvp.Value;
            }
        }

        return Regex.Replace(endPoint, @"\{(\w+)\}",
            match =>
            {
                var key = match.Groups[1].Value;

                if (!allValues.TryGetValue(key, out var value))
                {
                    throw new KeyNotFoundException(
                        $"Key '{key}' not found in any JSON file.");
                }

                return value;
            });
    }

    private static string ResolveValue(string value, Dictionary<string, string> configValues)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim();

        // $key -> read from config
        if (value.StartsWith('$', StringComparison.Ordinal))
            return ConfigReaderNew.GetValue(value[1..]);

        // {key} -> use cached endpoint value
        if (value.StartsWith('{', StringComparison.Ordinal) && value.EndsWith('}'))
        {
            var key = value[1..^1];
            return EndpointRequestHelper.GetCachedValue(key);
        }

        // try configValues first, then treat as literal
        if (configValues != null && configValues.TryGetValue(value, out var resolved))
            return resolved;

        return value;
    }

    [GeneratedRegex(@"\{(\w+)\}", RegexOptions.Compiled)]
    private static partial Regex UrlSegmentRegex();
}
