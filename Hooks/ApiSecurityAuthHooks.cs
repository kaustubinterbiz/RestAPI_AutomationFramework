using EnterpriseApiAutomationFramework.Core.Helpers;
using EnterpriseApiAutomationFramework.Core.Security.Authentication;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.Hooks;

/// <summary>
/// Ensures ApiSecurityAuthMatrix.xlsx exists before Security Authentication scenarios run.
/// </summary>
[Binding]
public class ApiSecurityAuthHooks
{
    [BeforeTestRun]
    public static void EnsureApiSecurityAuthMatrixExists()
    {
        ApiSecurityAuthBootstrap.EnsureWorkbook();
        ExcelConfigBootstrap.EnsureLoginRequestWorkbook();
        ExcelConfigBootstrap.EnsureRequestEndPointWorkbook();
    }

    [BeforeScenario("@Authentication", "@Security")]
    public void EnsureMatrixBeforeSecurityAuthenticationScenario()
    {
        ApiSecurityAuthBootstrap.EnsureWorkbook();
    }
}
