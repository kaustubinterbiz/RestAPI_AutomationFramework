using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Interfaces;

public interface IRestBuilder
{
    IRestBuilder WithGet<T>(string endpoint);
    IRestBuilder WithPost<T>(string endpoint);
    IRestBuilder WithPut<T>(string endpoint);
    IRestBuilder WithPatch<T>(string endpoint);
    IRestBuilder WithDelete<T>(string endpoint);
    IRestBuilder WithHeader(string key, string value);
    IRestBuilder WithQueryParameter(string key, string value);
    IRestBuilder WithUrlSegment(string key, string value);
    IRestBuilder WithBody(object body);
    IRestBuilder WithRequest(RestRequest request);

    Task<RestResponse> ExecuteAsync();
    Task<T?> ExecuteAsync<T>();
}
