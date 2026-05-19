using EnterpriseApiAutomationFramework.Core.Validators;
using EnterpriseApiAutomationFramework.Drivers;
using RestSharp;
using TechTalk.SpecFlow;

namespace EnterpriseApiAutomationFramework.StepDefinitions;

[Binding]
public class UserSteps
{
    private readonly UserDriver _driver;
    private RestResponse? response;

    public UserSteps()
    {
        _driver = new UserDriver();
    }

    [When(@"User sends GET request")]
    public async Task GetRequest()
    {
        response = await _driver.GetUsers("appsettings.json", "EndpointJson", "get");
    }

    [When(@"User sends POST request")]
    public async Task PostRequest()
    {
        response = await _driver.CreateUser(new
        {
            UserName = "username",
            Password = "password"
        });
    }

    [When(@"User sends PUT request")]
    public async Task PutRequest()
    {
        response = await _driver.UpdateUser(new
        {
            name = "Updated User",
            job = "Lead QA"
        });
    }

    [When(@"User sends PATCH request")]
    public async Task PatchRequest()
    {
        response = await _driver.PatchUser(new
        {
            job = "Manager"
        });
    }

    [When(@"User sends DELETE request")]
    public async Task DeleteRequest()
    {
        response = await _driver.DeleteUser();
    }

    [Then(@"Status code should be (.*)")]
    public void ValidateStatusCode(int statusCode)
    {
        ResponseValidator.ValidateStatusCode(response!, statusCode);
    }
}