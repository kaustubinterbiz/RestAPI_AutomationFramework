using Newtonsoft.Json;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Reporting;

public static class ApiRequestPayloadHelper
{
    public static string? Extract(RestRequest request)
    {
        var bodyParam = request.Parameters
            .FirstOrDefault(p => p.Type == ParameterType.RequestBody);

        if (bodyParam?.Value == null)
            return null;

        return bodyParam.Value switch
        {
            string json => json,
            _ => JsonConvert.SerializeObject(bodyParam.Value, Formatting.Indented)
        };
    }
}
