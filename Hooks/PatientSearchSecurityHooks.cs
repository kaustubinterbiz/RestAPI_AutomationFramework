using EnterpriseApiAutomationFramework.Core.Security.PatientSearch;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.Hooks;

[Binding]
public class PatientSearchSecurityHooks
{
    [BeforeTestRun(Order = 7)]
    public static void BeforePatientSearchSecurityTestRun()
    {
        PatientSearchSecurityBootstrap.EnsureInfrastructure();
    }
}
