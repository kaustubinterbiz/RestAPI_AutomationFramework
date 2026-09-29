using EnterpriseApiAutomationFramework.Core.Security.Patient;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.Hooks;

[Binding]
public class PatientSecurityHooks
{
    [BeforeTestRun(Order = 6)]
    public static void BeforePatientSecurityTestRun()
    {
        PatientSecurityBootstrap.EnsurePatientInfrastructure();
    }
}
