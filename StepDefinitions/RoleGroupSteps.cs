using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Drivers;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.StepDefinitions;

[Binding]
public class RoleGroupSteps
{
    private const string ExpectedAddMemberMessage = "User exists in same organization";

    private readonly ScenarioContext _context;
    private readonly UserDriver _driver;

    public RoleGroupSteps(ScenarioContext context)
    {
        _context = context;
        _driver = new UserDriver();
    }

    [When(@"User executes add member register flow for role group ""(.*)""")]
    public async Task WhenUserExecutesAddMemberRegisterFlowForRoleGroup(string parentRole)
    {
        RoleGroupExecutionTracker.Begin(_context);
        var childRoles = ExcelConfigReader.GetChildRoles(parentRole);

        foreach (var role in childRoles)
        {
            try
            {
                await RoleGroupFlowExecutor.ExecuteRegisterFlowForRoleAsync(_driver, _context, role);
                RoleGroupExecutionTracker.RecordSuccess(_context, role);
            }
            catch (Exception ex)
            {
                RoleGroupExecutionTracker.RecordFailure(_context, role, ex);
                throw;
            }
        }
    }

    [When(@"User executes add multiple member by excel flow for role group ""(.*)""")]
    public async Task WhenUserExecutesAddMultipleMemberByExcelFlowForRoleGroup(string parentRole)
    {
        RoleGroupExecutionTracker.Begin(_context);
        var childRoles = ExcelConfigReader.GetChildRoles(parentRole);

        foreach (var role in childRoles)
        {
            try
            {
                await RoleGroupFlowExecutor.ExecuteAddMultipleMemberByExcelFlowForRoleAsync(
                    _driver,
                    _context,
                    role,
                    ExpectedAddMemberMessage);
                RoleGroupExecutionTracker.RecordSuccess(_context, role);
            }
            catch (Exception ex)
            {
                RoleGroupExecutionTracker.RecordFailure(_context, role, ex);
                throw;
            }
        }
    }

    [Then(@"all role executions in the group should pass")]
    public void ThenAllRoleExecutionsInTheGroupShouldPass() =>
        RoleGroupExecutionTracker.AssertAllPassed(_context);
}
