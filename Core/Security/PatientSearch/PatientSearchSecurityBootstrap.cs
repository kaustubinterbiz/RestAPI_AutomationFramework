using EnterpriseApiAutomationFramework.Core.Helpers;

namespace EnterpriseApiAutomationFramework.Core.Security.PatientSearch;

public static class PatientSearchSecurityBootstrap
{
    private static int _initialized;

    public static void EnsureInfrastructure()
    {
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
            return;

        ExcelConfigBootstrap.EnsureRequestEndPointWorkbook();

        EnsureEndpoint(
            PatientSearchSecurityConstants.EndpointKey,
            "api/v2/Patient/Search/1");

        EnsureEndpoint(
            PatientSearchSecurityConstants.EndpointKeyDynamicPath,
            "api/v2/Patient/Search/{pathSegment}");
    }

    private static void EnsureEndpoint(string key, string path)
    {
        try
        {
            _ = ExcelConfigReader.GetEndpoint(key);
        }
        catch
        {
            ExcelConfigWriter.UpsertBySearchColumn(
                TestConfigDefaults.EndpointExcelFile,
                TestConfigDefaults.EndpointSheet,
                TestConfigDefaults.KeyColumn,
                key,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [TestConfigDefaults.ValueColumn] = path
                });
        }
    }
}
