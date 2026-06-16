using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Builders;
using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Core.Validators;
using EnterpriseApiAutomationFramework.Drivers;
using NUnit.Framework;
using Reqnroll;
using RestSharp;
using static Reqnroll.Analytics.ReqnrollFeatureUseEvent;

namespace EnterpriseApiAutomationFramework.StepDefinitions;

[Binding]
public class UserSteps
{
    private readonly ScenarioContext _context;
    private readonly UserDriver _driver;

    public UserSteps(ScenarioContext context)
    {
        _context = context;
        _driver = new UserDriver();
    }

    private void SaveResponse(RestResponse res)
    {
        TokenContext.SetLastResponse(_context, res);
    }

    [When(@"User sends GET request")]
    public async Task GetRequest() =>
        SaveResponse(await _driver.GetAsync());

    [When(@"User sends GET request on ""(.*)"" base url")]
    public async Task GetRequestOnBaseUrl(string baseUrlType)
    {
        ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        SaveResponse(await _driver.GetAsync());
    }

    [When(@"User sends GET request for feature ""(.*)""")]
    public async Task GetRequestForFeature(string featureName)
    {
        ApiHostStepHelper.ApplyFeatureName(featureName);
        SaveResponse(await _driver.GetAsync());
    }

    [When(@"User sends GET request for feature ""(.*)"" with cached id")]
    public async Task GetRequestForFeatureWithCachedId(string featureName)
    {
        ApiHostStepHelper.ApplyFeatureName(featureName);
        SaveResponse(await _driver.GetAsync());
    }

    [When(@"User sends GET request for feature ""(.*)"" using endpoint ""(.*)""")]
    public async Task GetRequestForFeatureWithEndpoint(string featureName, string endpointKey)
    {
        ApiHostStepHelper.ApplyFeatureName(featureName);
        SaveResponse(await _driver.GetAsync(endpointKey));
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
            : await _driver.PostFromConfigAsync("create_product", "JsonBody", "productCreateBody"));
    }

    [When("User sends POST request on {string} base url with {string}")]
    public async Task WhenUserSendsPOSTRequestOnBaseUrlWith(string baseUrlType,string loginRoleKey)
    {
        var host = ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);

        SaveResponse(host == ApiHost.Auth
                ? await _driver.LoginAsync(loginRoleKey)
                : await _driver.PostFromConfigAsync("create_product","JsonBody","productCreateBody"));
    }

    [When(@"User sends flexible GET request on ""(.*)"" base url for endpoint ""(.*)"" with headers ""(.*)""")]
    public async Task FlexibleGetRequestWithHeaders(string baseUrlType, string endpointKey, string headerKeys)
    {
        var host = ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        SaveResponse(await _driver.SendFlexibleRequestAsync(
            configFile: "appsettings.json",
            urlPlaceholderKeys: null,
            targetValue: null,
            headerKeys: ToOptionalKey(headerKeys),
            queryParamKeys: null,
            urlSegmentKeys: null,
            method: Method.Get,
            endpointKey: endpointKey,
            host: host));
    }

    [When(@"User sends flexible ""(.*)"" request on ""(.*)"" base url for endpoint ""(.*)"" with url placeholders ""(.*)"" target ""(.*)"" headers ""(.*)"" query params ""(.*)"" body ""(.*)""")]
    public async Task FlexibleGetRequestWithAllParts(
        Method methodType,
        string baseUrlType,
        string endpointKey,
        string urlPlaceholderKeys,
        string targetValue,
        string headerKeys,
        string queryParamKeys,
        string body)
    {
        var host = ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        ApiGetRequestOptions? options = null;
        var bodyKey = ToOptionalKey(body);

        if (string.Equals(endpointKey, "addMultipleMemberByExcel", StringComparison.OrdinalIgnoreCase))
        {
            ConfigReaderNew.LoadConfig("appsettings.json");
            var excelBody = AddMultipleMemberByExcelBuilder.BuildFromExcel(
                fileName: "Sample_File_Member.xlsx",
                sheetName: "Sheet1",
                businessUnitId: ConfigReaderNew.GetValue("BusinessUnitId"),
                addExistingUser: false);

            options = ApiGetRequestOptions.Create().SetBody(excelBody);
            bodyKey = null;
        }

        SaveResponse(await _driver.SendFlexibleRequestAsync(
            configFile: "appsettings.json",
            urlPlaceholderKeys: ToOptionalKey(urlPlaceholderKeys),
            targetValue: ToOptionalKey(targetValue),
            headerKeys: ToOptionalKey(headerKeys),
            queryParamKeys: ToOptionalKey(queryParamKeys),
            urlSegmentKeys: null,
            method: methodType,
            endpointKey: endpointKey,
            host: host,
            options: options,
            bodyKey: bodyKey));
    }

    private static string? ToOptionalKey(string value) =>
        string.IsNullOrWhiteSpace(value) || value == "-" ? null : value.Trim();

    [Then(@"Confirm the existing logged_in user is exist ""(.*)""")]
    public async Task ThenConfirmTheExistingLogged_InUserIsExist(string baseUrlType)
    {
        var host = ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        SaveResponse(host == ApiHost.Api
                ? await _driver.DynamicRequestPassMethod("appsettings.json", null, null, "CacheId", null, Method.Get, "getExistingUser")
                : await _driver.DynamicRequestPassMethod("appsettings.json", "EmailId", null, null, "CacheId", Method.Get,"getExistingUser"));
    }

    [Then("Confirm the Email logged_in user is exist {string}")]
    public async Task ThenConfirmTheEmailLogged_InUserIsExist(string baseUrlType)
    {
        var host = ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        SaveResponse(host == ApiHost.Api
               ? await _driver.DynamicRequestPassMethod("appsettings.json", "ValidateEmail", "EmailId", "CacheId", null, Method.Get, "getExistingUser")
               : await _driver.DynamicRequestPassMethod("appsettings.json", "EmailId", null, null, "CacheId", Method.Get, "getExistingUser"));
    }

    [Then("Confirm the User exist in the Same Organization {string}")]
    public async Task ThenConfirmTheUserExistInTheSameOrganization(string validateCheckExistingEmail)
    {
        var host = ApiHostStepHelper.ApplyBaseUrlType("Api");
        
        SaveResponse(await _driver.SendFlexibleRequestAsync(
            configFile: "appsettings.json",
            urlPlaceholderKeys: ToOptionalKey("-"),
            targetValue: ToOptionalKey("-"),
            headerKeys: ToOptionalKey("CacheId"),
            queryParamKeys: ToOptionalKey($"{validateCheckExistingEmail}, ValidateBusinessUnitId"),
            urlSegmentKeys: null,
            method: Method.Get,
            endpointKey: "getCheckAvability",
            host: host, bodyKey: null));

    }

    [Then("validate the response for the existing user in the same organization {string} and {string}")]
    public async Task ThenValidateTheResponseForTheExistingUserInTheSameOrganizationAnd(string placeholder, string target)
    {
        var response = TokenContext.GetLastResponse(_context);
        StoreInfo.SaveExistingUserFromResponse(response.Content);
        ConfigReaderNew.LoadConfig("appsettings.json");
        string value1 = ConfigReaderNew.GetValue("IsAvailableUserEmail");
        bool.TryParse(value1, out bool result1);
        string value2 = ConfigReaderNew.GetValue("IsSameBusinessUnitMemebr");
        bool.TryParse(value2, out bool result2);
        if (result1 == false && result2 == true)
        {
            _context["GetExistingUserEmail"] = false;
            Console.WriteLine($"Expected IsAvailableUserEmail to be exist and also in same Business Unit Id '{_context["GetExistingUserEmail"].ToString()}'.");
        }
        else if (result1 == false && result2 == false)
        {
            _context["GetExistingUserEmail"] = true;
            Console.WriteLine($"Expected IsAvailableUserEmail to be exist but not in same Business Unit Id '{_context["GetExistingUserEmail"].ToString()}'.");
        }
        else
        {
            _context["GetExistingUserEmail"] = true;
            Console.WriteLine($"Expected IsAvailableUserEmail to be not exist, create a new organization");
        }
        bool getExistingUserEmail = (bool)_context["GetExistingUserEmail"];
        if (getExistingUserEmail)
        {
            var host = ApiHostStepHelper.ApplyBaseUrlType("Api");

            SaveResponse(await _driver.SendFlexibleRequestAsync(
                configFile: "appsettings.json",
                urlPlaceholderKeys: ToOptionalKey(placeholder),
                targetValue: ToOptionalKey(target),
                headerKeys: ToOptionalKey("CacheId"),
                queryParamKeys: ToOptionalKey("-"),
                urlSegmentKeys: null,
                method: Method.Get,
                endpointKey: "getExistingUser1",
                host: host, bodyKey: null
                ));

            response = TokenContext.GetLastResponse(_context);
           //StoreInfo.SaveExistingUserFromResponse(response.Content);
        }
        else { Console.WriteLine("Already Email exist and in the same business unit id"); }
    }

    [When(@"User sends POST request for feature ""(.*)""")]
    public async Task PostRequestForFeature(string featureName)
    {
        var host = ApiHostStepHelper.ApplyFeatureName(featureName);
        SaveResponse(host == ApiHost.Auth
            ? await _driver.LoginAsync()
            : await _driver.PostFromConfigAsync("create_product", "JsonBody", "productCreateBody"));
    }

    [When("User sends POST request to create")]
    public async Task WhenUserSendsPOSTRequestToCreate() =>
        SaveResponse(await _driver.PostFromConfigAsync("create_product", "JsonBody", "productCreateBody"));

    [When(@"User sends POST request to create on ""(.*)"" base url")]
    public async Task PostCreateOnBaseUrl(string baseUrlType)
    {
        ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        SaveResponse(await _driver.PostFromConfigAsync("create_product", "JsonBody", "productCreateBody"));
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
