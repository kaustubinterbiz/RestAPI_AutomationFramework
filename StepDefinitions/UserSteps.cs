using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Validators;
using EnterpriseApiAutomationFramework.Drivers;
using RestSharp;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.StepDefinitions;

[Binding]
public class UserSteps
{
    private readonly ScenarioContext _context;
    private readonly UserDriver _driver;
    private RestResponse? response;

    public UserSteps(ScenarioContext context)
    {
        _context = context;
        _driver = new UserDriver();
    }

    private void SaveResponse(RestResponse res)
    {
        response = res;
        TokenContext.SetLastResponse(_context, res);
    }

    [When(@"User sends GET request")]
    public async Task GetRequest() =>
        SaveResponse(await _driver.GetUsers("appsettings.json", "EndpointJson", "get"));

    [When(@"User sends GET request on ""(.*)"" base url")]
    public async Task GetRequestOnBaseUrl(string baseUrlType)
    {
        ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        SaveResponse(await _driver.GetUsers("appsettings.json", "EndpointJson", "get"));
    }

    [When(@"User sends GET request for feature ""(.*)""")]
    public async Task GetRequestForFeature(string featureName)
    {
        ApiHostStepHelper.ApplyFeatureName(featureName);
        SaveResponse(await _driver.GetFromConfig(endpointKey: "get"));
    }

    [When(@"User sends GET request for feature ""(.*)"" with cached id")]
    public async Task GetRequestForFeatureWithCachedId(string featureName)
    {
        ApiHostStepHelper.ApplyFeatureName(featureName);
        SaveResponse(await _driver.GetFromConfig(endpointKey: "get"));
    }

    [When(@"User sends GET request for feature ""(.*)"" using endpoint ""(.*)""")]
    public async Task GetRequestForFeatureWithEndpoint(string featureName, string endpointKey)
    {
        ApiHostStepHelper.ApplyFeatureName(featureName);
        SaveResponse(await _driver.GetFromConfig(endpointKey: endpointKey));
    }

    [When(@"User sends POST request")]
    public async Task PostRequest() =>
        SaveResponse(await _driver.LoginAsync());

    [When(@"User sends POST request on ""(.*)"" base url")]
    public async Task PostRequestOnBaseUrl(string baseUrlType)
    {
        var host = ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        SaveResponse(host == ApiHost.Auth
            ? await _driver.LoginAsync()
            : await _driver.PostUser(
                "appsettings.json",
                "EndpointJson",
                "create_product",
                "JsonBody",
                "productCreateBody"));
    }

    [When(@"User sends POST request for feature ""(.*)""")]
    public async Task PostRequestForFeature(string featureName)
    {
        var host = ApiHostStepHelper.ApplyFeatureName(featureName);
        SaveResponse(host == ApiHost.Auth
            ? await _driver.LoginAsync()
            : await _driver.PostUser(
                "appsettings.json",
                "EndpointJson",
                "create_product",
                "JsonBody",
                "productCreateBody"));
    }

    [When("User sends POST request to create")]
    public async Task WhenUserSendsPOSTRequestToCreate() =>
        SaveResponse(await _driver.PostUser(
            "appsettings.json",
            "EndpointJson",
            "create_product",
            "JsonBody",
            "productCreateBody"));

    [When(@"User sends POST request to create on ""(.*)"" base url")]
    public async Task PostCreateOnBaseUrl(string baseUrlType)
    {
        ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        SaveResponse(await _driver.PostUser(
            "appsettings.json",
            "EndpointJson",
            "create_product",
            "JsonBody",
            "productCreateBody"));
    }

    [When(@"User sends PUT request")]
    public async Task PutRequest() =>
        SaveResponse(await _driver.UpdateUser(new { name = "Updated User", job = "Lead QA" }));

    [When(@"User sends PUT request on ""(.*)"" base url")]
    public async Task PutRequestOnBaseUrl(string baseUrlType)
    {
        ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        SaveResponse(await _driver.UpdateUser(new { name = "Updated User", job = "Lead QA" }));
    }

    [When(@"User sends PATCH request")]
    public async Task PatchRequest() =>
        SaveResponse(await _driver.PatchUser(new { job = "Manager" }));

    [When(@"User sends PATCH request on ""(.*)"" base url")]
    public async Task PatchRequestOnBaseUrl(string baseUrlType)
    {
        ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        SaveResponse(await _driver.PatchUser(new { job = "Manager" }));
    }

    [When(@"User sends DELETE request")]
    public async Task DeleteRequest() =>
        SaveResponse(await _driver.DeleteUser());

    [When(@"User sends DELETE request on ""(.*)"" base url")]
    public async Task DeleteRequestOnBaseUrl(string baseUrlType)
    {
        ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        SaveResponse(await _driver.DeleteUser());
    }

    [Then(@"Status code should be (.*)")]
    public void ValidateStatusCode(int statusCode) =>
        ResponseValidator.ValidateStatusCode(TokenContext.GetLastResponse(_context), statusCode);

    [Then("Status should be (.*)")]
    public void ThenStatusShouldBe(string status) =>
        ResponseValidator.ValidateStatus(TokenContext.GetLastResponse(_context), status);
}
