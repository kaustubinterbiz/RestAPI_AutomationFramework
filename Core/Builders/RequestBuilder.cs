using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Configurations;
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
}
