using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Core.Security.Patient;
using EnterpriseApiAutomationFramework.Hooks;
using Reqnroll;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Security.PatientSearch;

public sealed class PatientSearchSecurityRunner
{
    private readonly PatientSearchSecurityAuthHelper _authHelper = new();
    private readonly PatientSearchSecurityRequestExecutor _executor = new();

    public async Task LoginWithSessionAsync(ScenarioContext context, string role)
    {
        AuthorizationExecutionTracker.Begin(context);
        await _authHelper.PrepareSessionAsync(context, role);
    }

    public async Task SendSearchAsync(
        ScenarioContext context,
        string baseUrlType,
        string firstName,
        string lastName,
        string dob,
        string pathSegment = "1")
    {
        if (!context.TryGetValue(AuthorizationExecutionTracker.ResultsKey, out _))
            AuthorizationExecutionTracker.Begin(context);

        var host = ResolveHost(baseUrlType);
        var (fn, ln, d) = PatientSearchSecurityTestDataConfig.ResolveDemographics(firstName, lastName, dob);
        var body = PatientSearchSecurityBodyBuilder.Build(fn, ln, d);

        var auth = ResolveSearchAuthContext(host);
        var response = await _executor.ExecutePostAsync(host, body, auth, pathSegment);

        context.Set(response, PatientSearchSecurityConstants.LastResponseKey);
    }

    public async Task SendSearchWithAuthProblemAsync(
        ScenarioContext context,
        string baseUrlType,
        string authProblemLabel)
    {
        AuthorizationExecutionTracker.Begin(context);

        var authVariant = PatientSearchSecurityAuthProblemMapper.MapToAuthVariant(authProblemLabel);
        var host = ResolveHost(baseUrlType);
        var body = PatientSearchSecurityBodyBuilder.Build(
            PatientSearchSecurityTestDataConfig.DefaultFirstName,
            PatientSearchSecurityTestDataConfig.DefaultLastName,
            PatientSearchSecurityTestDataConfig.DefaultDob);

        var baseline = TokenManager.AccessToken;
        var auth = _authHelper.BuildAuthContext(authVariant, baseline);

        var response = await _executor.ExecutePostAsync(host, body, auth, pathSegment: "1");
        context.Set(response, PatientSearchSecurityConstants.LastResponseKey);
        context.Set($"Auth:{authProblemLabel}", PatientSearchSecurityConstants.LastLabelKey);
    }

    public void ValidateSearchOutcome(ScenarioContext context, string outcome)
    {
        var response = GetResponse(context);
        var label = $"Outcome:{outcome}";
        RecordValidation(context, label, response, 200, () =>
        {
            var errors = new List<string>();
            Collect(
                PatientSearchResponseAssert.ValidateSearchOutcome(
                    response.Content,
                    (int)response.StatusCode,
                    outcome),
                errors);
            return errors;
        });
    }

    public void ValidateUnauthorizedRejection(ScenarioContext context)
    {
        var response = GetResponse(context);
        var label = context.TryGetValue(PatientSearchSecurityConstants.LastLabelKey, out string? lv) && !string.IsNullOrWhiteSpace(lv)
            ? lv
            : "UnauthorizedRejection";

        RecordValidation(context, label, response, 401, () =>
        {
            var errors = new List<string>();
            var actual = (int)response.StatusCode;
            Collect(PatientSearchResponseAssert.ValidateAuthRejection(actual), errors);
            Collect(PatientSearchResponseAssert.ValidateNoInternalLeak(response.Content), errors);
            return errors;
        });
    }

    public void ValidateSearchSucceedsForUser(ScenarioContext context)
    {
        var response = GetResponse(context);
        RecordValidation(context, "SearchSucceedsForUser", response, 200, () =>
        {
            var errors = new List<string>();
            Collect(
                PatientSearchResponseAssert.ValidateSearchSucceedsForUser(
                    response.Content,
                    (int)response.StatusCode),
                errors);
            return errors;
        });
    }

    public void ValidateOtherOrgPatientNotInResponse(ScenarioContext context)
    {
        var response = GetResponse(context);
        var orgBId = PatientSecurityTestDataConfig.OrgBPatientId;
        RecordValidation(context, "OtherOrgPatientNotInResponse", response, (int)response.StatusCode, () =>
        {
            var errors = new List<string>();
            Collect(
                PatientSearchResponseAssert.ValidateOtherOrgPatientNotInResponse(response.Content, orgBId),
                errors);
            return errors;
        });
    }

    public void ValidateSafeErrorResponse(ScenarioContext context, int expectedStatus)
    {
        var response = GetResponse(context);
        var label = $"InputValidation:{expectedStatus}";
        RecordValidation(context, label, response, expectedStatus, () =>
        {
            var errors = new List<string>();
            Collect(PatientSearchResponseAssert.ValidateHttpStatus((int)response.StatusCode, expectedStatus), errors);
            Collect(PatientSearchResponseAssert.ValidateNoInternalLeak(response.Content), errors);
            return errors;
        });
    }

    private static RestResponse GetResponse(ScenarioContext context)
    {
        if (!context.TryGetValue(PatientSearchSecurityConstants.LastResponseKey, out RestResponse response))
            throw new InvalidOperationException("No Patient Search response stored. Send request step must run first.");

        return response;
    }

    private static void RecordValidation(
        ScenarioContext context,
        string label,
        RestResponse response,
        int expectedStatus,
        Func<List<string>> collectErrors)
    {
        var errors = collectErrors().Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
        var actual = (int)response.StatusCode;
        if (errors.Count == 0)
        {
            AuthorizationExecutionTracker.RecordSuccess(context, label, actual, expectedStatus);
            return;
        }

        AuthorizationExecutionTracker.RecordFailure(
            context,
            label,
            actual,
            expectedStatus,
            string.Join("; ", errors));
    }

    private static void Collect(string? error, List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(error))
            errors.Add(error);
    }

    private static ApiHost ResolveHost(string baseUrlType)
    {
        var host = ApiHostResolver.ResolveFromKey(baseUrlType);
        ApiHostHooks.SetHost(host);
        return host;
    }

    private static PatientSearchSecurityAuthContext ResolveSearchAuthContext(ApiHost _) =>
        new() { SendAuthorizationHeader = true };
}
