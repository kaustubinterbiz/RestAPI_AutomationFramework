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
    public static (string Endpoint,Dictionary<string, string> UrlSegments) ResolveEndpoint(string endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        if (endpoint.StartsWith('$'))
        {
            endpoint = ConfigReaderNew.GetValue(endpoint[1..]);
        }

        if (!endpoint.Contains('{'))
        {
            return (
                endpoint,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        var urlSegments =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in UrlSegmentPattern.Matches(endpoint))
        {
            var key = match.Groups[1].Value;

            if (urlSegments.ContainsKey(key))
            {
                continue;
            }

            var value = EndpointRequestHelper.GetCachedValue(key);

            if (!string.IsNullOrWhiteSpace(value))
            {
                urlSegments[key] = value;
            }
        }

        // Query string case -> replace directly
        if (endpoint.Contains('?', StringComparison.Ordinal))
        {
            endpoint = ReplacePlaceholders(endpoint);

            return (endpoint, urlSegments);
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
            if (!string.IsNullOrEmpty(value))
            {
                value = Uri.UnescapeDataString(value);
            }
            result = result.Replace(match.Value, value, StringComparison.Ordinal);
        }

        return result;
    }

    public static Dictionary<string, string>? BuildQueryParams(
     string? queryParam,
     string jsonFileName,
     string jsonKey)
    {
        if (string.IsNullOrWhiteSpace(queryParam))
            return null;

        // Read data from JSON
        var configValues = ConfigReaderNew.ReadJson<Dictionary<string, string>>(
            jsonFileName);
        if (configValues == null)
        {
            configValues = new Dictionary<string, string>();
        }

        var dict = new Dictionary<string, string>();

        var items = queryParam.Split(
            ',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var item in items)
        {
            var part = item.Trim();

            // CASE 1: key=value
            if (part.Contains('='))
            {
                var kv = part.Split('=', 2);

                var key = kv[0].Trim();
                var value = kv[1].Trim();

                dict[key] = ResolveValue(value, configValues);
            }
            // CASE 2: key only
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

    private static string ResolveValue(
        string value,
        Dictionary<string, string> configValues)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim();

        // $key -> read from config
        if (value.StartsWith("$", StringComparison.Ordinal))
            return ConfigReaderNew.GetValue(value[1..]);

        // {key} -> cached value
        if (value.StartsWith("{", StringComparison.Ordinal)
            && value.EndsWith('}'))
        {
            var key = value[1..^1];
            return EndpointRequestHelper.GetCachedValue(key);
        }

        // lookup in json data
        if (configValues.TryGetValue(value, out var resolved))
            return resolved;

        return value;
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

    public static string ResolveUrlPlaceholders(
    string url,
    Dictionary<string, string>? values = null,
    string? targetKey = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        return Regex.Replace(url, @"\{(.*?)\}", match =>
        {
            var placeholderKey = match.Groups[1].Value?.Trim();

            if (string.IsNullOrWhiteSpace(placeholderKey))
                return match.Value;

            // Placeholder and targetKey must match
            if (!string.IsNullOrWhiteSpace(targetKey) &&
                placeholderKey.Equals(targetKey, StringComparison.OrdinalIgnoreCase))
            {
                // Use first available value from dictionary
                var replacementValue = values?.Values.FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(replacementValue))
                {
                    return replacementValue;
                }
            }

            return match.Value;
        });
    }

    [GeneratedRegex(@"\{(\w+)\}", RegexOptions.Compiled)]
    private static partial Regex UrlSegmentRegex();
}
