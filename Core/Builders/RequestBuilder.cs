using RestSharp;
using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Models.Request;

namespace EnterpriseApiAutomationFramework.Core.Builders;

public class RequestBuilder
{
    public RestRequest BuildLoginRequest(string endpoint, LoginRequest credentials)
    {
        var request = new RestRequest(endpoint, Method.Post);

        request.AddHeader("cookie", "x-ms-cpim-geo=NA");

        foreach (var parameter in credentials.ToFormParameters())
        {
            request.AddParameter(parameter.Key, parameter.Value, ParameterType.GetOrPost);
        }

        return request;
    }

    public RestRequest BuildRequest(
        string endpoint,
        Method method,
        object? body = null,
        Dictionary<string, string>? headers = null,
        Dictionary<string, string>? queryParams = null,
        Dictionary<string, string>? urlSegments = null,
        bool authorizationRequired = true)
    {
        var request = new RestRequest(endpoint, method);

        if (urlSegments != null)
        {
            foreach (var segment in urlSegments)
            {
                request.AddUrlSegment(segment.Key, segment.Value);
            }
        }

        if (authorizationRequired && !string.IsNullOrEmpty(TokenManager.AccessToken))
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
}
