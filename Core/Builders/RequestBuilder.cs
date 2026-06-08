using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Models.Request;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Builders;

public class RequestBuilder
{
    public RestRequest BuildLoginRequest(string endpoint, LoginRequest credentials, string? bearerToken = null)
    {
        var request = new RestRequest(endpoint, Method.Post);

        request.AddHeader("cookie", "x-ms-cpim-geo=NA");

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.AddHeader("Authorization", $"Bearer {bearerToken}");
        }

        foreach (var parameter in credentials.ToFormParameters())
        {
            request.AddParameter(parameter.Key, parameter.Value, ParameterType.GetOrPost);
        }

        return request;
    }

    /// <summary>POST with bearer only (no ROPC body) — for invalid/expired token checks.</summary>
    public RestRequest BuildBearerOnlyPostRequest(string endpoint, string bearerToken)
    {
        var request = new RestRequest(endpoint, Method.Post);
        request.AddHeader("cookie", "x-ms-cpim-geo=NA");
        request.AddHeader("Authorization", $"Bearer {bearerToken}");
        return request;
    }

    public RestRequest BuildRequest(string endpoint, Method method, object? body = null,
        Dictionary<string, string>? headers = null, Dictionary<string, string>? queryParams = null, Dictionary<string, string>? urlSegments = null,
        bool authorizationRequired = true, string? explicitBearerToken = null, bool explicitBearerTokenProvided = false)
    {
        var request = new RestRequest(endpoint, method);

        if (urlSegments != null)
        {
            foreach (var segment in urlSegments)
            {
                request.AddUrlSegment(segment.Key, segment.Value);
            }
        }

        if (explicitBearerTokenProvided)
        {
            request.AddHeader("Authorization", $"Bearer {explicitBearerToken}");
        }
        else if (authorizationRequired && !string.IsNullOrEmpty(TokenManager.AccessToken))
        {
            request.AddHeader("Authorization", $"Bearer {TokenManager.AccessToken}");
        }

        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.AddHeader(header.Key, header.Value);
            }
        }

        if (queryParams != null)
        {
            foreach (var param in queryParams)
            {
                request.AddQueryParameter(param.Key, param.Value);
            }
        }

        if (body != null)
        {
            if (body is string jsonBody && !string.IsNullOrWhiteSpace(jsonBody))
            {
                request.AddStringBody(jsonBody, ContentType.Json);
            }
            else
            {
                request.AddJsonBody(body);
            }
        }

        return request;
    }

    public RestRequest DynamicRequestBuild(string endpoint, Method method, object? body = null,
       Dictionary<string, string>? headers = null, Dictionary<string, string>? queryParams = null, Dictionary<string, string>? urlSegments = null,
       bool authorizationRequired = true, string? explicitBearerToken = null, bool explicitBearerTokenProvided = false)
    {
        var request = new RestRequest(endpoint, method);
        if (urlSegments != null)
        {
            foreach (var segment in urlSegments)
            {
                request.AddUrlSegment(segment.Key, segment.Value);
            }
        }

        if (explicitBearerTokenProvided)
        {
            request.AddHeader("Authorization", $"Bearer {explicitBearerToken}");
        }

        else if (authorizationRequired && !string.IsNullOrEmpty(TokenManager.AccessToken))
        {
            request.AddHeader("Authorization", $"Bearer {TokenManager.AccessToken}");
        }

        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.AddHeader(header.Key, header.Value);
            }
        }

        if (queryParams != null)
        {
            foreach (var param in queryParams)
            {
                request.AddQueryParameter(param.Key, param.Value);
            }
        }

        if (body != null)
        {
            if (body is string jsonBody && !string.IsNullOrWhiteSpace(jsonBody))
            {
                request.AddStringBody(jsonBody, ContentType.Json);
            }
            else
            {
                request.AddJsonBody(body);
            }
        }
        return request;
    }

    public static Dictionary<string, string>? RequestParamBuilder(string? keys, string jsonFile)
    {
        if (string.IsNullOrWhiteSpace(keys))
            return null;

        ConfigReaderNew.LoadConfig(jsonFile);

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var items = keys.Split(',', StringSplitOptions.RemoveEmptyEntries);
          
        foreach (var item in items)
        {
            var key = item.Trim();

            if (string.IsNullOrWhiteSpace(key))
                continue;

            var value = ConfigReaderNew.GetValue(key);

            if (!string.IsNullOrWhiteSpace(value))
            {
                dict[key] = value;
            }
        }

        return dict.Count > 0 ? dict : null;
    }

    //    public RestRequest BuildFlexibleDynamicRequest(
    //string endpoint,
    //Method method,
    //FlexibleRequestOptions? options = null)
    //    {
    //        options ??= new FlexibleRequestOptions();
    //        var request = new RestRequest(endpoint, method);

    //        // URL segments (optional)
    //        if (options.UrlSegments is { Count: > 0 })
    //        {
    //            foreach (var segment in options.UrlSegments)
    //            {
    //                if (!string.IsNullOrWhiteSpace(segment.Key)
    //                    && !string.IsNullOrWhiteSpace(segment.Value))
    //                {
    //                    request.AddUrlSegment(segment.Key, segment.Value);
    //                }
    //            }
    //        }

    //        // Token (optional)
    //        if (options.BearerTokenProvided)
    //        {
    //            request.AddHeader("Authorization", $"Bearer {options.BearerToken}");
    //        }
    //        else if (options.AuthorizationRequired
    //                 && !string.IsNullOrWhiteSpace(TokenManager.AccessToken))
    //        {
    //            request.AddHeader("Authorization", $"Bearer {TokenManager.AccessToken}");
    //        }

    //        // Multiple headers (optional)
    //        if (options.Headers is { Count: > 0 })
    //        {
    //            foreach (var header in options.Headers)
    //            {
    //                if (!string.IsNullOrWhiteSpace(header.Key)
    //                    && !string.IsNullOrWhiteSpace(header.Value))
    //                {
    //                    request.AddHeader(header.Key, header.Value);
    //                }
    //            }
    //        }

    //        // Multiple query params (optional)
    //        if (options.QueryParams is { Count: > 0 })
    //        {
    //            foreach (var param in options.QueryParams)
    //            {
    //                if (!string.IsNullOrWhiteSpace(param.Key)
    //                    && !string.IsNullOrWhiteSpace(param.Value))
    //                {
    //                    request.AddQueryParameter(param.Key, param.Value);
    //                }
    //            }
    //        }

    //        // Body (optional — sirf ek)
    //        if (options.Body != null)
    //        {
    //            if (options.Body is string jsonBody && !string.IsNullOrWhiteSpace(jsonBody))
    //            {
    //                request.AddStringBody(jsonBody, ContentType.Json);
    //            }
    //            else
    //            {
    //                request.AddJsonBody(options.Body);
    //            }
    //        }

    //        return request;
    //    }

    public RestRequest BuildFlexibleDynamicRequest(
    string endpoint,
    Method method,
    FlexibleRequestOptions? options = null)
    {
        options ??= new FlexibleRequestOptions();

        var queryParams = options.QueryParams;

        // Endpoint mein {} hai → sirf placeholder VALUES replace; query param names same
        if (endpoint.Contains('{', StringComparison.Ordinal)
            && queryParams is { Count: > 0 })
        {
            endpoint = EndpointHelper.ResolveEndpointPlaceholders(endpoint, queryParams);
            queryParams = null;   // local variable — OK
        }

        var request = new RestRequest(endpoint, method);

        // URL segments (optional)
        if (options.UrlSegments is { Count: > 0 })
        {
            foreach (var segment in options.UrlSegments)
            {
                if (!string.IsNullOrWhiteSpace(segment.Key)
                    && !string.IsNullOrWhiteSpace(segment.Value))
                {
                    request.AddUrlSegment(segment.Key, segment.Value);
                }
            }
        }

        // Token (optional)
        if (options.BearerTokenProvided)
        {
            request.AddHeader("Authorization", $"Bearer {options.BearerToken}");
        }
        else if (options.AuthorizationRequired
                 && !string.IsNullOrWhiteSpace(TokenManager.AccessToken))
        {
            request.AddHeader("Authorization", $"Bearer {TokenManager.AccessToken}");
        }

        // Multiple headers (optional)
        if (options.Headers is { Count: > 0 })
        {
            foreach (var header in options.Headers)
            {
                if (!string.IsNullOrWhiteSpace(header.Key)
                    && !string.IsNullOrWhiteSpace(header.Value))
                {
                    request.AddHeader(header.Key, header.Value);
                }
            }
        }

        // Query params — sirf jab endpoint mein {} na ho
        if (queryParams is { Count: > 0 })
        {
            foreach (var param in queryParams)
            {
                if (!string.IsNullOrWhiteSpace(param.Key)
                    && !string.IsNullOrWhiteSpace(param.Value))
                {
                    request.AddQueryParameter(param.Key, param.Value);
                }
            }
        }

        // Body (optional)
        if (options.Body != null)
        {
            if (options.Body is string jsonBody && !string.IsNullOrWhiteSpace(jsonBody))
            {
                request.AddStringBody(jsonBody, ContentType.Json);
            }
            else
            {
                request.AddJsonBody(options.Body);
            }
        }

        return request;
    }

    /// <summary>
    /// Builds dictionary from comma-separated keys.
    /// Formats:
    ///   null/empty        -> null (skip)
    ///   "EmailId,CacheId" -> { EmailId: value, CacheId: value }
    ///   "emailId=EmailId,cacheId=CacheId" -> param/header name alag, config key alag
    /// </summary>
    public static Dictionary<string, string>? ResolvePartsFromConfig(
        string? keys,
        string configFile = "appsettings.json",
        bool useCachedValues = true)
        {
        if (string.IsNullOrWhiteSpace(keys))
            return null;

        ConfigReaderNew.LoadConfig(configFile);
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in keys.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var part = item.Trim();
            if (string.IsNullOrWhiteSpace(part))
                continue;

            string mapKey;
            string configKey;

            // emailId=EmailId  OR  sirf EmailId
            if (part.Contains('='))
            {
                var kv = part.Split('=', 2);
                mapKey = kv[0].Trim();
                configKey = kv[1].Trim();
            }
            else
            {
                mapKey = part;
                configKey = part;
            }

            var value = useCachedValues
                ? Core.Helpers.EndpointRequestHelper.GetCachedValue(configKey)
                : ConfigReaderNew.GetValue(configKey);

            if (!string.IsNullOrWhiteSpace(value))
                dict[mapKey] = value;
        }

        return dict.Count > 0 ? dict : null;
    }
}
