using EnterpriseApiAutomationFramework.Core.Security.PatientSearch;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.StepDefinitions;

[Binding]
public class ApiSecurityPatientSearchSteps
{
    private readonly ScenarioContext _context;
    private readonly PatientSearchSecurityRunner _runner = new();

    public ApiSecurityPatientSearchSteps(ScenarioContext context) => _context = context;

    [When(@"the user ""([^""]*)"" is logged in with session ready for Patient Search")]
    public async Task WhenTheUserIsLoggedInWithSessionReadyForPatientSearch(string role)
    {
        await _runner.LoginWithSessionAsync(_context, role);
    }

    [When(@"the user searches patients on ""([^""]*)"" with first name ""([^""]*)"" last name ""([^""]*)"" date of birth ""([^""]*)""")]
    public async Task WhenTheUserSearchesPatientsOnApimWithDemographics(
        string baseUrlType,
        string firstName,
        string lastName,
        string dob)
    {
        await _runner.SendSearchAsync(_context, baseUrlType, firstName, lastName, dob);
    }

    [When(@"the user searches patients on ""([^""]*)"" page ""([^""]*)"" with first name ""([^""]*)"" last name ""([^""]*)"" date of birth ""([^""]*)""")]
    public async Task WhenTheUserSearchesPatientsOnApimWithPathAndDemographics(
        string baseUrlType,
        string pathSegment,
        string firstName,
        string lastName,
        string dob)
    {
        await _runner.SendSearchAsync(_context, baseUrlType, firstName, lastName, dob, pathSegment);
    }

    [When(@"the user searches patients on ""([^""]*)"" with ""([^""]*)""")]
    public async Task WhenTheUserSearchesPatientsOnApimWithAuthProblem(
        string baseUrlType,
        string authProblem)
    {
        await _runner.SendSearchWithAuthProblemAsync(_context, baseUrlType, authProblem);
    }

    [Then(@"the search result should be ""([^""]*)""")]
    public void ThenTheSearchResultShouldBe(string outcome)
    {
        _runner.ValidateSearchOutcome(_context, outcome);
    }

    [Then(@"the API should reject the request as unauthorized")]
    public void ThenTheApiShouldRejectTheRequestAsUnauthorized()
    {
        _runner.ValidateUnauthorizedRejection(_context);
    }

    [Then(@"the search should succeed for this user")]
    public void ThenTheSearchShouldSucceedForThisUser()
    {
        _runner.ValidateSearchSucceedsForUser(_context);
    }

    [Then(@"the other organization's patient id must not appear in the response")]
    public void ThenTheOtherOrganizationsPatientIdMustNotAppearInTheResponse()
    {
        _runner.ValidateOtherOrgPatientNotInResponse(_context);
    }

    [Then(@"the API should respond with status (.*) and not expose internal errors")]
    public void ThenTheApiShouldRespondWithStatusAndNotExposeInternalErrors(int expectedStatus)
    {
        _runner.ValidateSafeErrorResponse(_context, expectedStatus);
    }
}
