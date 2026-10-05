using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Core.Security.Patient;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.StepDefinitions;

[Binding]
public class ApiSecurityPatientSteps
{
    private readonly ScenarioContext _context;
    private readonly PatientSecurityExecutor _executor = new();

    public ApiSecurityPatientSteps(ScenarioContext context) => _context = context;

    [When(@"Patient Security runs test ""(.*)""")]
    public async Task WhenPatientSecurityRunsTest(string testCaseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testCaseId);

               var entry = PatientSecurityMatrixReader.FindByTestCaseId(testCaseId)
            ?? throw new InvalidOperationException(
                $"No Patient security row found for TestCaseId '{testCaseId}'.");

        AuthorizationExecutionTracker.Begin(_context);
        await _executor.ExecuteRowAsync(_context, entry);
    }

    [When(@"Patient Security runs all authentication tests from Excel")]
    public async Task WhenPatientSecurityRunsAllAuthenticationTestsFromExcel()
    {
        var rows = PatientSecurityMatrixReader.GetAuthenticationRows(enabledOnly: true);
        await _executor.ExecuteAllAsync(_context, rows);
    }

    [When(@"Patient Security runs all IDOR tests from Excel")]
    public async Task WhenPatientSecurityRunsAllIdorTestsFromExcel()
    {
        var rows = PatientSecurityMatrixReader.GetIdorRows(enabledOnly: true);
        await _executor.ExecuteAllAsync(_context, rows);
    }

    [When(@"Patient Security runs all input validation tests from Excel")]
    public async Task WhenPatientSecurityRunsAllInputValidationTestsFromExcel()
    {
        var rows = PatientSecurityMatrixReader.GetInputValidationRows(enabledOnly: true);
        await _executor.ExecuteAllAsync(_context, rows);
    }

    [When(@"Patient Security runs all P0 tests from Excel")]
    public async Task WhenPatientSecurityRunsAllP0TestsFromExcel()
    {
        var rows = PatientSecurityMatrixReader.GetAllPatientRows(enabledOnly: true);
        await _executor.ExecuteAllAsync(_context, rows);
    }
}
