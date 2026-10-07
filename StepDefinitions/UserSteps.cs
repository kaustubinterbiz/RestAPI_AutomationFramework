using AventStack.ExtentReports;
using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Builders;
using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Core.Security.Patient;
using EnterpriseApiAutomationFramework.Core.Security.Reporting;
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
        if (string.Equals(loginRoleKey, "BannerRole", StringComparison.OrdinalIgnoreCase))
            ExcelConfigBootstrap.EnsureBannerRole();

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

    [When(@"User sends flexible ""([^""]*)"" request on ""([^""]*)"" base url for endpoint ""([^""]*)"" with url placeholders ""([^""]*)"" target ""([^""]*)"" headers ""([^""]*)"" query params ""([^""]*)"" body ""([^""]*)""$")]
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
            var excelBodyJson = AddMultipleMemberByExcelBuilder.BuildJsonFromExcel(
                fileName: AddMultipleMemberByExcelDefaults.FileName,
                sheetName: AddMultipleMemberByExcelDefaults.InputSheetName,
                businessUnitId: EndpointRequestHelper.GetCachedValue("BusinessUnitId"),
                addExistingUser: false);

            options = ApiGetRequestOptions.Create().SetBody(excelBodyJson);
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

    [When(@"User sends flexible ""([^""]*)"" request on ""([^""]*)"" base url for endpoint ""([^""]*)"" with url placeholders ""([^""]*)"" target ""([^""]*)"" headers ""([^""]*)"" query params ""([^""]*)"" body ""([^""]*)"" url segments ""([^""]*)"" token ""([^""]*)""")]
    public async Task FlexibleRequestWithSegmentsAndToken(
        Method methodType,
        string baseUrlType,
        string endpointKey,
        string urlPlaceholderKeys,
        string targetValue,
        string headerKeys,
        string queryParamKeys,
        string body,
        string urlSegments,
        string tokenModeText)
    {
        EnsurePatientEndpointExcel(endpointKey);

        var host = ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        var tokenMode = FlexibleTokenModeParser.Parse(tokenModeText);
        ApiGetRequestOptions? options = null;
        var bodyKey = ToOptionalKey(body);

        if (string.Equals(bodyKey, PatientListRequestHelper.BodySheetKey, StringComparison.OrdinalIgnoreCase))
        {
            options = ApiGetRequestOptions.Create().SetBody(PatientListRequestHelper.ResolvePatientListBodyJson());
            bodyKey = null;
        }
        else if (string.Equals(bodyKey, GetFhirDataRequestHelper.BodySheetKey, StringComparison.OrdinalIgnoreCase))
        {
            options = ApiGetRequestOptions.Create().SetBody(GetFhirDataRequestHelper.ResolveGetFhirDataBodyJson());
            bodyKey = null;
        }
        else if (string.Equals(bodyKey, GetFeatureBasedDataRequestHelper.BodySheetKey, StringComparison.OrdinalIgnoreCase))
        {
            options = ApiGetRequestOptions.Create().SetBody(GetFeatureBasedDataRequestHelper.ResolveGetFeatureBasedDataBodyJson());
            bodyKey = null;
        }
        else if (string.Equals(bodyKey, PatientSearchRequestHelper.BodySheetKey, StringComparison.OrdinalIgnoreCase))
        {
            options = ApiGetRequestOptions.Create().SetBody(PatientSearchRequestHelper.ResolvePatientSearchBodyJson());
            bodyKey = null;
        }

        IReadOnlyDictionary<string, string>? urlSegmentOverrides = null;
        string? urlSegmentKeys = ToOptionalKey(urlSegments);
        if (string.Equals(endpointKey, "patientList", StringComparison.OrdinalIgnoreCase))
        {
            urlSegmentOverrides = PatientListRequestHelper.ResolvePatientListUrlSegments();
            urlSegmentKeys = null;
        }
        else if (string.Equals(endpointKey, GetFhirDataRequestHelper.EndpointKey, StringComparison.OrdinalIgnoreCase))
        {
            urlSegmentOverrides = GetFhirDataRequestHelper.ResolveGetFhirDataUrlSegments();
            urlSegmentKeys = null;
        }
        else if (string.Equals(endpointKey, GetFeatureBasedDataRequestHelper.EndpointKey, StringComparison.OrdinalIgnoreCase))
        {
            urlSegmentOverrides = GetFeatureBasedDataRequestHelper.ResolveGetFeatureBasedDataUrlSegments();
            urlSegmentKeys = null;
        }

        SaveResponse(await _driver.SendFlexibleRequestAsync(
            configFile: "appsettings.json",
            urlPlaceholderKeys: ToOptionalKey(urlPlaceholderKeys),
            targetValue: ToOptionalKey(targetValue),
            headerKeys: ToOptionalKey(headerKeys),
            queryParamKeys: ToOptionalKey(queryParamKeys),
            urlSegmentKeys: urlSegmentKeys,
            method: methodType,
            endpointKey: endpointKey,
            host: host,
            options: options,
            bodyKey: bodyKey,
            tokenMode: tokenMode,
            urlSegmentOverrides: urlSegmentOverrides));
    }

    [When(@"User sends Patient Search request on ""([^""]*)"" with insurance ""([^""]*)"" sort ""([^""]*)"" by ""([^""]*)"" token ""([^""]*)""")]
    public async Task WhenUserSendsPatientSearchRequestWithFilters(
        string baseUrlType,
        string insurance,
        string sort,
        string by,
        string tokenModeText)
    {
        EnsurePatientSearchExcel();

        var host = ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        var tokenMode = FlexibleTokenModeParser.Parse(tokenModeText);
        var options = ApiGetRequestOptions.Create()
            .SetBody(PatientSearchRequestHelper.ResolvePatientSearchBodyJson(insurance, sort, by));

        SaveResponse(await _driver.SendFlexibleRequestAsync(
            configFile: "appsettings.json",
            urlPlaceholderKeys: null,
            targetValue: null,
            headerKeys: "CacheId",
            queryParamKeys: null,
            urlSegmentKeys: null,
            method: Method.Post,
            endpointKey: PatientSearchRequestHelper.EndpointKey,
            host: host,
            options: options,
            bodyKey: null,
            tokenMode: tokenMode,
            urlSegmentOverrides: null));
    }

    [When(@"User sends GetFeatureBasedData request on ""([^""]*)"" for data type ""([^""]*)"" with token ""([^""]*)""")]
    public async Task WhenUserSendsGetFeatureBasedDataRequest(
        string baseUrlType,
        string dataType,
        string tokenModeText)
    {
        ExcelConfigBootstrap.EnsureBannerRole();
        EnsureGetFeatureBasedDataExcel();

        var host = ApiHostStepHelper.ApplyBaseUrlType(baseUrlType);
        var tokenMode = FlexibleTokenModeParser.Parse(tokenModeText);
        var urlSegmentOverrides = GetFeatureBasedDataRequestHelper.ResolveGetFeatureBasedDataUrlSegments(
            dataTypeOverride: dataType);
        var options = ApiGetRequestOptions.Create()
            .SetBody(GetFeatureBasedDataRequestHelper.ResolveGetFeatureBasedDataBodyJson());

        SaveResponse(await _driver.SendFlexibleRequestAsync(
            configFile: "appsettings.json",
            urlPlaceholderKeys: null,
            targetValue: null,
            headerKeys: "CacheId",
            queryParamKeys: null,
            urlSegmentKeys: null,
            method: Method.Post,
            endpointKey: GetFeatureBasedDataRequestHelper.EndpointKey,
            host: host,
            options: options,
            bodyKey: null,
            tokenMode: tokenMode,
            urlSegmentOverrides: urlSegmentOverrides));
    }

    private static int _patientListExcelInitialized;
    private static int _getFhirDataExcelInitialized;
    private static int _getFeatureBasedDataExcelInitialized;
    private static int _patientSearchExcelInitialized;

    private static void EnsurePatientEndpointExcel(string endpointKey)
    {
        if (string.Equals(endpointKey, "patientList", StringComparison.OrdinalIgnoreCase))
            EnsurePatientListExcel();
        else if (string.Equals(endpointKey, GetFhirDataRequestHelper.EndpointKey, StringComparison.OrdinalIgnoreCase))
            EnsureGetFhirDataExcel();
        else if (string.Equals(endpointKey, GetFeatureBasedDataRequestHelper.EndpointKey, StringComparison.OrdinalIgnoreCase))
            EnsureGetFeatureBasedDataExcel();
        else if (string.Equals(endpointKey, PatientSearchRequestHelper.EndpointKey, StringComparison.OrdinalIgnoreCase))
            EnsurePatientSearchExcel();
    }

    private static void EnsurePatientListExcel()
    {
        if (Interlocked.CompareExchange(ref _patientListExcelInitialized, 1, 0) == 0)
        {
            ExcelConfigBootstrap.EnsureRequestEndPointWorkbook();
            ExcelConfigBootstrap.EnsureRequestBodyWorkbook();

            try
            {
                _ = ExcelConfigReader.GetEndpoint("patientList");
            }
            catch
            {
                ExcelReader.AddRow(
                    TestConfigDefaults.EndpointExcelFile,
                    TestConfigDefaults.EndpointSheet,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TestConfigDefaults.KeyColumn] = "patientList",
                        [TestConfigDefaults.ValueColumn] = "api/v2/Patient/{businessunitId}/Patients"
                    });
            }
        }

        UpsertPatientListBodyTemplate();
    }

    private static void EnsureGetFhirDataExcel()
    {
        if (Interlocked.CompareExchange(ref _getFhirDataExcelInitialized, 1, 0) == 0)
        {
            ExcelConfigBootstrap.EnsureRequestEndPointWorkbook();
            ExcelConfigBootstrap.EnsureRequestBodyWorkbook();

            try
            {
                _ = ExcelConfigReader.GetEndpoint(GetFhirDataRequestHelper.EndpointKey);
            }
            catch
            {
                ExcelReader.AddRow(
                    TestConfigDefaults.EndpointExcelFile,
                    TestConfigDefaults.EndpointSheet,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TestConfigDefaults.KeyColumn] = GetFhirDataRequestHelper.EndpointKey,
                        [TestConfigDefaults.ValueColumn] = "api/v2/Patient/{businessunitId}/GetFhirData"
                    });
            }
        }

        UpsertGetFhirDataBodyTemplate();
    }

    private static void EnsureGetFeatureBasedDataExcel()
    {
        if (Interlocked.CompareExchange(ref _getFeatureBasedDataExcelInitialized, 1, 0) == 0)
        {
            ExcelConfigBootstrap.EnsureRequestEndPointWorkbook();
            ExcelConfigBootstrap.EnsureRequestBodyWorkbook();

            const string endpointPath = "api/v2/Patient/{patientId}/{dataType}";
            ExcelConfigWriter.UpsertBySearchColumn(
                TestConfigDefaults.EndpointExcelFile,
                TestConfigDefaults.EndpointSheet,
                TestConfigDefaults.KeyColumn,
                GetFeatureBasedDataRequestHelper.EndpointKey,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [TestConfigDefaults.ValueColumn] = endpointPath
                });
        }

        UpsertGetFeatureBasedDataBodyTemplate();
    }

    private static void EnsurePatientSearchExcel()
    {
        if (Interlocked.CompareExchange(ref _patientSearchExcelInitialized, 1, 0) == 0)
        {
            ExcelConfigBootstrap.EnsureRequestEndPointWorkbook();
            ExcelConfigBootstrap.EnsureRequestBodyWorkbook();

            ExcelConfigWriter.UpsertBySearchColumn(
                TestConfigDefaults.EndpointExcelFile,
                TestConfigDefaults.EndpointSheet,
                TestConfigDefaults.KeyColumn,
                PatientSearchRequestHelper.EndpointKey,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [TestConfigDefaults.ValueColumn] = "api/v2/Patient/Search/1"
                });
        }

        UpsertPatientSearchBodyTemplate();
        UpsertPatientSearchFilterRows();
    }

    private static void UpsertPatientListBodyTemplate()
    {
        var filePath = FileUploadHelper.GetFilePath(TestConfigDefaults.BodyExcelFile);
        ExcelReader.WithWorkbook(filePath, workbook =>
        {
            var sheetName = PatientListRequestHelper.BodySheetKey;
            if (!workbook.TryGetWorksheet(sheetName, out var sheet))
            {
                sheet = workbook.AddWorksheet(sheetName);
                sheet.Cell(1, 1).Value = TestConfigDefaults.FieldColumn;
                sheet.Cell(1, 2).Value = TestConfigDefaults.ValueColumn;
            }

            sheet.Cell(2, 1).Value = TestConfigDefaults.BodyRawJsonMarker;
            sheet.Cell(2, 2).Value = PatientListRequestHelper.PatientListBodyTemplate;
        }, save: true);
    }

    private static void UpsertGetFhirDataBodyTemplate()
    {
        var filePath = FileUploadHelper.GetFilePath(TestConfigDefaults.BodyExcelFile);
        ExcelReader.WithWorkbook(filePath, workbook =>
        {
            var sheetName = GetFhirDataRequestHelper.BodySheetKey;
            if (!workbook.TryGetWorksheet(sheetName, out var sheet))
            {
                sheet = workbook.AddWorksheet(sheetName);
                sheet.Cell(1, 1).Value = TestConfigDefaults.FieldColumn;
                sheet.Cell(1, 2).Value = TestConfigDefaults.ValueColumn;
            }

            sheet.Cell(2, 1).Value = TestConfigDefaults.BodyRawJsonMarker;
            sheet.Cell(2, 2).Value = GetFhirDataRequestHelper.GetFhirDataBodyTemplate;
        }, save: true);
    }

    private static void UpsertGetFeatureBasedDataBodyTemplate()
    {
        var filePath = FileUploadHelper.GetFilePath(TestConfigDefaults.BodyExcelFile);
        ExcelReader.WithWorkbook(filePath, workbook =>
        {
            var sheetName = GetFeatureBasedDataRequestHelper.BodySheetKey;
            if (!workbook.TryGetWorksheet(sheetName, out var sheet))
            {
                sheet = workbook.AddWorksheet(sheetName);
                sheet.Cell(1, 1).Value = TestConfigDefaults.FieldColumn;
                sheet.Cell(1, 2).Value = TestConfigDefaults.ValueColumn;
            }

            sheet.Cell(2, 1).Value = TestConfigDefaults.BodyRawJsonMarker;
            sheet.Cell(2, 2).Value = GetFeatureBasedDataRequestHelper.GetFeatureBasedDataBodyTemplate;
        }, save: true);
    }

    private static void UpsertPatientSearchBodyTemplate()
    {
        var filePath = FileUploadHelper.GetFilePath(TestConfigDefaults.BodyExcelFile);
        ExcelReader.WithWorkbook(filePath, workbook =>
        {
            var sheetName = PatientSearchRequestHelper.BodySheetKey;
            if (!workbook.TryGetWorksheet(sheetName, out var sheet))
            {
                sheet = workbook.AddWorksheet(sheetName);
                sheet.Cell(1, 1).Value = TestConfigDefaults.FieldColumn;
                sheet.Cell(1, 2).Value = TestConfigDefaults.ValueColumn;
            }

            sheet.Cell(2, 1).Value = TestConfigDefaults.BodyRawJsonMarker;
            sheet.Cell(2, 2).Value = PatientSearchRequestHelper.PatientSearchBodyTemplate;
        }, save: true);
    }

    private static void UpsertPatientSearchFilterRows()
    {
        var filePath = FileUploadHelper.GetFilePath(TestConfigDefaults.BodyExcelFile);
        ExcelReader.WithWorkbook(filePath, workbook =>
        {
            var sheetName = PatientSearchRequestHelper.FilterSheetKey;
            if (!workbook.TryGetWorksheet(sheetName, out var sheet))
            {
                sheet = workbook.AddWorksheet(sheetName);
                sheet.Cell(1, 1).Value = TestConfigDefaults.KeyColumn;
                sheet.Cell(1, 2).Value = TestConfigDefaults.ValueColumn;
            }
        }, save: true);

        foreach (var (key, value) in PatientSearchRequestHelper.DefaultFilterValues)
        {
            ExcelConfigWriter.UpsertBySearchColumn(
                TestConfigDefaults.BodyExcelFile,
                PatientSearchRequestHelper.FilterSheetKey,
                TestConfigDefaults.KeyColumn,
                key,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [TestConfigDefaults.ValueColumn] = value
                });
        }
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
    public void ValidateStatusCode(int statusCode)
    {
        _context.Set(statusCode, SecurityReportingConstants.LastExpectedStatusKey);
        ResponseValidator.ValidateStatusCode(TokenContext.GetLastResponse(_context), statusCode);
    }

    [Then(@"cross-org user must be denied GetFeatureBasedData access with status code (.*)")]
    public void ThenCrossOrgUserMustBeDeniedGetFeatureBasedDataAccess(int expectedStatus)
    {
        _context.Set(expectedStatus, SecurityReportingConstants.LastExpectedStatusKey);

        var response = TokenContext.GetLastResponse(_context);
        var actualStatus = (int)response.StatusCode;

        if (actualStatus == expectedStatus)
            return;

        var hospitalPatientId = GetFeatureBasedDataRequestHelper.ResolvePatientId();
        var content = response.Content ?? string.Empty;
        var hasPhiLeak = content.Contains(hospitalPatientId, StringComparison.OrdinalIgnoreCase)
            && content.Contains("\"FirstName\":", StringComparison.Ordinal)
            && !content.Contains("\"FirstName\":null", StringComparison.Ordinal);

        var phiNote = PatientSecurityPhiAssert.ValidateNoSensitiveLeak(content);
        if (hasPhiLeak || phiNote != null)
        {
            Assert.Fail(
                $"SECURITY BREACH: Cross-org user received HTTP {actualStatus} with hospital patient data " +
                $"(patientId={hospitalPatientId}). Expected HTTP {expectedStatus}.{(phiNote != null ? $" {phiNote}" : string.Empty)}");
        }

        ResponseValidator.ValidateStatusCode(response, expectedStatus);
    }

    [Then("Status should be (.*)")]
    public void ThenStatusShouldBe(string status) =>
        ResponseValidator.ValidateStatus(TokenContext.GetLastResponse(_context), status);

    [Then("Validate the Status should be {string}")]
    public void ThenValidateTheStatusShouldBe(string expectedStatus) =>
        AddMultipleMemberByExcelValidator.ValidateAllStatusesFromExcel(
            AddMultipleMemberByExcelDefaults.FileName,
            AddMultipleMemberByExcelDefaults.ResponseSheetName,
            expectedStatus);

    [Then(@"Validate all member statuses in excel file ""(.*)"" sheet ""(.*)"" should be ""(.*)""")]
    public void ThenValidateAllMemberStatusesInExcel(string fileName, string sheetName, string expectedStatus) =>
        AddMultipleMemberByExcelValidator.ValidateAllStatusesFromExcel(fileName, sheetName, expectedStatus);

    [Then("Status for AddMultipleMemberByExcel should be {string}")]
    public void ThenStatusForAddMultipleMemberByExcelShouldBe(string expectedStatus)
    {
        var response = TokenContext.GetLastResponse(_context);
        AddMultipleMemberByExcelValidator.ValidateAllMemberStatusesFromResponse(response.Content, expectedStatus);
    }

}
