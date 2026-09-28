using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Core.Security.Authentication;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.StepDefinitions;

/// <summary>
/// Step bindings for API Security Authentication (JWT-gate) ExcelLoop scenarios.
/// Reuses <see cref="AuthorizationExecutionTracker.AssertAllPassed"/> via existing Then wording.
/// </summary>
[Binding]
public class ApiSecurityAuthSteps
{
    private readonly ScenarioContext _context;
    private readonly ApiSecurityAuthExecutor _executor;

    public ApiSecurityAuthSteps(ScenarioContext context)
    {
        _context = context;
        _executor = new ApiSecurityAuthExecutor();
    }

    [When(@"API Security executes all authentication tests from Excel")]
    public async Task WhenApiSecurityExecutesAllAuthenticationTestsFromExcel()
    {
        await _executor.ExecuteAllAuthenticationTestsAsync(_context);
    }

    [When(@"API Security runs authentication test ""(.*)""")]
    [When(@"API Security executes authentication test ""(.*)""")]
    public async Task WhenApiSecurityExecutesAuthenticationTest(string testCaseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testCaseId);

        var entry = ApiSecurityAuthMatrixReader.GetAuthenticationRows(enabledOnly: false)
            .FirstOrDefault(r => string.Equals(r.TestCaseId, testCaseId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No Authentication row found for TestCaseId '{testCaseId}' in " +
                $"'{ApiSecurityAuthConstants.MatrixExcelFile}'.");

        AuthorizationExecutionTracker.Begin(_context);
        var helper = new ApiSecurityAuthHelper();

        try
        {
            string? baseline = null;
            var needsLogin =
                !string.Equals(entry.ScenarioType, ApiSecurityAuthConstants.ScenarioNoAuthHeader, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(entry.ScenarioType, ApiSecurityAuthConstants.ScenarioEmptyBearer, StringComparison.OrdinalIgnoreCase);

            if (needsLogin)
                baseline = await helper.EnsureBaselineTokenAsync(_context, entry.Role);

            var response = await helper.ExecuteAsync(_context, entry, baseline);
            var actual = (int)response.StatusCode;

            if (entry.MatchesStatus(actual))
            {
                AuthorizationExecutionTracker.RecordSuccess(
                    _context,
                    entry.Label,
                    actual,
                    entry.ExpectedStatus);
            }
            else
            {
                AuthorizationExecutionTracker.RecordFailure(
                    _context,
                    entry.Label,
                    actual,
                    entry.ExpectedStatus,
                    $"Expected HTTP [{entry.ExpectedStatusDisplay}], got {actual}. Body={response.Content}");
            }
        }
        catch (Exception ex)
        {
            AuthorizationExecutionTracker.RecordFailure(
                _context,
                entry.Label,
                null,
                entry.ExpectedStatus,
                ex.Message);
        }
    }
}
