using EnterpriseApiAutomationFramework.Core.Helpers;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.Hooks;

[Binding]
public class RoleGroupHooks
{
    private readonly ScenarioContext _scenarioContext;

    public RoleGroupHooks(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeTestRun]
    public static void ValidateAddMemberRoleGroupConfiguration()
    {
        ExcelConfigBootstrap.EnsureRoleGroupsSheet();

        var childRoles = ExcelConfigReader.GetAddMemberChildRoles();
        ExcelConfigReader.ValidateChildRolesExistInRolesSheet(childRoles);
    }

    [BeforeScenario("@RoleOutline")]
    public void ValidateOutlineRoleExistsInExcelGroup()
    {
        if (!_scenarioContext.ScenarioInfo.Arguments.Contains("Role"))
            return;

        var role = _scenarioContext.ScenarioInfo.Arguments["Role"]?.ToString();
        if (string.IsNullOrWhiteSpace(role))
            return;

        var childRoles = ExcelConfigReader.GetAddMemberChildRoles();
        if (!childRoles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Scenario Outline role '{role}' is not configured under " +
                $"'{TestConfigDefaults.DefaultAddMemberRoleGroup}' in Excel sheet " +
                $"'{TestConfigDefaults.RoleGroupsSheet}'. Excel roles: {string.Join(", ", childRoles)}");
        }

        ExcelConfigReader.ValidateChildRolesExistInRolesSheet(new[] { role });
    }

    [BeforeScenario("@RoleGroupLoop")]
    public void ValidateRoleGroupLoopConfiguration()
    {
        ExcelConfigBootstrap.EnsureRoleGroupsSheet();
        var childRoles = ExcelConfigReader.GetAddMemberChildRoles();
        if (childRoles.Count == 0)
        {
            throw new InvalidOperationException(
                $"No roles found for '{TestConfigDefaults.DefaultAddMemberRoleGroup}' in Excel.");
        }

        ExcelConfigReader.ValidateChildRolesExistInRolesSheet(childRoles);
    }
}
