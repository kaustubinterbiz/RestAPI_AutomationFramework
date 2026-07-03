using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Builders;
using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Core.Validators;
using EnterpriseApiAutomationFramework.Drivers;
using EnterpriseApiAutomationFramework.StepDefinitions;
using Reqnroll;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Executes add-member flows for each child role under a parent role group (Excel RoleGroups sheet).
/// </summary>
public static class RoleGroupFlowExecutor
{
    private const string FeatureName = "User API Testing";

    public static async Task ExecuteRegisterFlowForRoleAsync(
        UserDriver driver,
        ScenarioContext context,
        string role)
    {
        SharedTokenProvider.InvalidateAllCaches();

        ApiHostStepHelper.ApplyBaseUrlType("Auth");
        var loginResponse = await driver.LoginAsync(role);
        ResponseValidator.ValidateStatus(loginResponse, "OK");
        ApiAuth.SaveTokenFromLoginResponse(context, loginResponse.Content);

        ApiHostStepHelper.ApplyFeatureName(FeatureName);
        var sessionResponse = await driver.GetAsync();
        ResponseValidator.ValidateStatusCode(sessionResponse, 200);
        StoreInfo.SaveSessionInfoFromResponse(sessionResponse.Content);

        ApiHostStepHelper.ApplyBaseUrlType("Api");
        var checkResponse = await driver.SendFlexibleRequestAsync(
            configFile: "appsettings.json",
            urlPlaceholderKeys: "ValidateCheckExistingEmail_Incorrect, BusinessUnitMemberId",
            targetValue: "ValidateCheckExistingEmail, ValidateBusinessUnitId",
            headerKeys: "CacheId",
            queryParamKeys: null,
            urlSegmentKeys: null,
            method: Method.Get,
            endpointKey: "getCheckAvability",
            host: ApiHost.Api,
            bodyKey: null);
        TokenContext.SetLastResponse(context, checkResponse);

        await ValidateExistingUserInSameOrganizationAsync(driver, context,
            "ValidateCheckExistingEmail_Incorrect", "ValidateCheckExistingEmail");

        var lastResponse = TokenContext.GetLastResponse(context);
        ResponseValidator.ValidateStatus(lastResponse, "NoContent");

        var registerResponse = await driver.SendFlexibleRequestAsync(
            configFile: "appsettings.json",
            urlPlaceholderKeys: null,
            targetValue: null,
            headerKeys: "CacheId",
            queryParamKeys: null,
            urlSegmentKeys: null,
            method: Method.Post,
            endpointKey: "post_Register",
            host: ApiHost.Api,
            bodyKey: "register_Body");
        ResponseValidator.ValidateStatusCode(registerResponse, 200);
    }

    public static async Task ExecuteAddMultipleMemberByExcelFlowForRoleAsync(
        UserDriver driver,
        ScenarioContext context,
        string role,
        string expectedStatusMessage)
    {
        SharedTokenProvider.InvalidateAllCaches();

        ApiHostStepHelper.ApplyBaseUrlType("Auth");
        var loginResponse = await driver.LoginAsync(role);
        ResponseValidator.ValidateStatus(loginResponse, "OK");
        ApiAuth.SaveTokenFromLoginResponse(context, loginResponse.Content);

        ApiHostStepHelper.ApplyFeatureName(FeatureName);
        var sessionResponse = await driver.GetAsync();
        ResponseValidator.ValidateStatusCode(sessionResponse, 200);
        StoreInfo.SaveSessionInfoFromResponse(sessionResponse.Content);

        ApiHostStepHelper.ApplyBaseUrlType("Api");
        ConfigReaderNew.LoadConfig("appsettings.json");
        var excelBodyJson = AddMultipleMemberByExcelBuilder.BuildJsonFromExcel(
            fileName: AddMultipleMemberByExcelDefaults.FileName,
            sheetName: AddMultipleMemberByExcelDefaults.InputSheetName,
            businessUnitId: EndpointRequestHelper.GetCachedValue("BusinessUnitId"),
            addExistingUser: false);

        var options = ApiGetRequestOptions.Create().SetBody(excelBodyJson);
        var uploadResponse = await driver.SendFlexibleRequestAsync(
            configFile: "appsettings.json",
            urlPlaceholderKeys: null,
            targetValue: null,
            headerKeys: "CacheId",
            queryParamKeys: null,
            urlSegmentKeys: null,
            method: Method.Post,
            endpointKey: "addMultipleMemberByExcel",
            host: ApiHost.Api,
            options: options,
            bodyKey: null);
        ResponseValidator.ValidateStatusCode(uploadResponse, 200);

        StoreInfo.SaveAddMultiMemberByExcelResponseToExcel(
            uploadResponse.Content,
            AddMultipleMemberByExcelDefaults.FileName,
            AddMultipleMemberByExcelDefaults.ResponseSheetName);
        StoreInfo.SaveAddMultiMemberByExcelFromResponse(uploadResponse.Content);

        AddMultipleMemberByExcelValidator.ValidateAllStatusesFromExcel(
            AddMultipleMemberByExcelDefaults.FileName,
            AddMultipleMemberByExcelDefaults.ResponseSheetName,
            expectedStatusMessage);
    }

    private static async Task ValidateExistingUserInSameOrganizationAsync(
        UserDriver driver,
        ScenarioContext context,
        string placeholder,
        string target)
    {
        var response = TokenContext.GetLastResponse(context);
        StoreInfo.SaveExistingUserFromResponse(response.Content);
        ConfigReaderNew.LoadConfig("appsettings.json");

        var value1 = ConfigReaderNew.GetValue("IsAvailableUserEmail");
        _ = bool.TryParse(value1, out var isAvailableUserEmail);
        var value2 = ConfigReaderNew.GetValue("IsSameBusinessUnitMemebr");
        _ = bool.TryParse(value2, out var isSameBusinessUnitMember);

        var getExistingUserEmail = isAvailableUserEmail || !isSameBusinessUnitMember;
        context["GetExistingUserEmail"] = getExistingUserEmail;

        if (!getExistingUserEmail)
            return;

        ApiHostStepHelper.ApplyBaseUrlType("Api");
        var existingUserResponse = await driver.SendFlexibleRequestAsync(
            configFile: "appsettings.json",
            urlPlaceholderKeys: placeholder,
            targetValue: target,
            headerKeys: "CacheId",
            queryParamKeys: null,
            urlSegmentKeys: null,
            method: Method.Get,
            endpointKey: "getExistingUser1",
            host: ApiHost.Api,
            bodyKey: null);

        TokenContext.SetLastResponse(context, existingUserResponse);
    }
}
