using RestSharp;
using EnterpriseApiAutomationFramework.Core.Authentication;

namespace EnterpriseApiAutomationFramework.Core.Builders;

public class RequestBuilder
{
    public RestRequest BuildRequest(
        string endpoint,
        Method method,
        object? body = null,
        Dictionary<string, string>? headers = null,
        Dictionary<string, string>? queryParams = null,
        bool authorizationRequired = true)
    {
        var request = new RestRequest(endpoint, method);

        request.AddHeader("Content-Type", "application/json");

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
            request.AddJsonBody(body);
        }

        return request;
    }
}