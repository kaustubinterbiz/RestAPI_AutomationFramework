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

    //public static string ResolveEndpointPlaceholders(
    //    string endpoint,
    //    Dictionary<string, string>? placeholderValues)
    //{
    //    if (string.IsNullOrWhiteSpace(endpoint))
    //        return endpoint;

    //    return UrlSegmentPattern.Replace(endpoint, match =>
    //    {
    //        var placeholderKey = match.Groups[1].Value;

    //        string? value = null;
    //        if (placeholderValues != null
    //            && placeholderValues.TryGetValue(placeholderKey, out var fromDict)
    //            && !string.IsNullOrWhiteSpace(fromDict))
    //        {
    //            value = fromDict;
    //        }
    //        else
    //        {
    //            value = EndpointRequestHelper.GetCachedValue(placeholderKey);
    //        }

    //        if (string.IsNullOrWhiteSpace(value))
    //            return match.Value;

    //        if (value.Contains('%'))
    //            value = Uri.UnescapeDataString(value);

    //        return value;
    //    });
    //}

    /// <summary>
    /// Comma-separated target aur value keys se ordered map banata hai.
    /// valueKeys[0] → targetKeys[0], valueKeys[1] → targetKeys[1], ...
    /// </summary>
    private static Dictionary<string, string> BuildSequentialPlaceholderMap(
    Dictionary<string, string>? values,
    string? valueKeys,
    string? targetKeys)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (values is not { Count: > 0 })
            return map;

        var valueKeyList = string.IsNullOrWhiteSpace(valueKeys)
            ? values.Keys.ToList()
            : valueKeys.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .ToList();

        if (string.IsNullOrWhiteSpace(targetKeys))
        {
            foreach (var kvp in values)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value))
                    map[kvp.Key] = kvp.Value;
            }
            return map;
        }

        var targetKeyList = targetKeys
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.Trim())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToList();

        var count = Math.Min(valueKeyList.Count, targetKeyList.Count);
        for (var i = 0; i < count; i++)
        {
            var configKey = valueKeyList[i];
            var targetKey = targetKeyList[i];

            if (!values.TryGetValue(configKey, out var value)
                || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            // configKey = endpoint {} name (ValidateBusinessUnitId) — hamesha map karo
            map[configKey] = value;

            // target alag ho to alias bhi add karo (BusinessUnitId)
            if (!targetKey.Equals(configKey, StringComparison.OrdinalIgnoreCase))
            {
                map[targetKey] = value;
            }
        }

        return map;
    }

    private static string ApplyPlaceholderMap(string url, Dictionary<string, string> replacementMap)
    {
        if (string.IsNullOrWhiteSpace(url) || replacementMap.Count == 0)
            return url;

        return Regex.Replace(url, @"\{(.*?)\}", match =>
        {
            var placeholderKey = match.Groups[1].Value?.Trim();
            if (string.IsNullOrWhiteSpace(placeholderKey))
                return match.Value;

            if (!replacementMap.TryGetValue(placeholderKey, out var value)
                || string.IsNullOrWhiteSpace(value))
            {
                return match.Value;
            }

            if (value.Contains('%'))
                value = Uri.UnescapeDataString(value);

            return value;
        });
    }

    /// <summary>
    /// Replaces endpoint {placeholders} using sequence mapping.
    /// valueKeys  = config keys (appsettings) — comma separated
    /// targetKeys = endpoint placeholder names — comma separated
    /// Example:
    ///   valueKeys  = "ValidateCheckExistingEmail_OtherBusinessUnitId, ValidateBusinessUnitId"
    ///   targetKeys = "ValidateCheckExistingEmail_OtherBusinessUnitId, ValidateBusinessUnitId"
    /// </summary>
    public static string ResolveUrlPlaceholders(
        string url,
        Dictionary<string, string>? values = null,
        string? targetKeys = null,
        string? valueKeys = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        var map = BuildSequentialPlaceholderMap(values, valueKeys, targetKeys);

        // Backward compatible: single target + single value dict
        if (map.Count == 0
            && values is { Count: 1 }
            && !string.IsNullOrWhiteSpace(targetKeys)
            && !targetKeys.Contains(','))
        {
            var onlyValue = values.Values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(onlyValue))
                map[targetKeys.Trim()] = onlyValue;
        }

        return ApplyPlaceholderMap(url, map);
    }

    /// <summary>
    /// Replaces {placeholderKey} values — dict key = placeholder name (query param mode).
    /// </summary>
    public static string ResolveEndpointPlaceholders(
        string endpoint,
        Dictionary<string, string>? placeholderValues)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return endpoint;

        if (placeholderValues is not { Count: > 0 })
            return endpoint;

        return ApplyPlaceholderMap(endpoint, placeholderValues);
    }

    [GeneratedRegex(@"\{(\w+)\}", RegexOptions.Compiled)]
    private static partial Regex UrlSegmentRegex();
}
