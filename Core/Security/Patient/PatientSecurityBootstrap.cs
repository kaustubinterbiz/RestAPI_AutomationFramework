using ClosedXML.Excel;
using EnterpriseApiAutomationFramework.Core.Helpers;

namespace EnterpriseApiAutomationFramework.Core.Security.Patient;

public static class PatientSecurityBootstrap
{
    private static int _initialized;

    public static void EnsurePatientInfrastructure()
    {
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
            return;

        ExcelConfigBootstrap.EnsureRequestEndPointWorkbook();
        ExcelConfigBootstrap.EnsureRequestBodyWorkbook();
        EnsurePatientEndpoints();
        EnsurePatientBodies();
    }

    private static void EnsurePatientEndpoints()
    {
        var endpoints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["patientGetFhirData"] = "api/v2/Patient/{businessunitId}/GetFhirData",
            ["patientGetByDataType"] = "api/v2/Patient/{patientId}/{dataType}",
            ["patientUpdateByDataType"] = "api/v2/Patient/{patientId}/{dataType}",
            ["patientFacesheet"] = "api/v2/Patient/Facesheet/{patientId}/{businessunitId}",
            ["patientDelete"] = "api/v2/Patient/Delete/{patientId}",
            ["patientAttribute"] = "api/v2/PatientAttribute",
            ["patientBulkProviders"] = "api/v2/PatientProvider/GetProvidersForMultiplePatientIds",
            ["patientEmrSearch"] = "api/v2/Patient/EMR/Search",
            ["patientEmrAddUpdate"] = "api/v2/Patient/EMR/AddUpdate"
        };

        foreach (var (key, path) in endpoints)
        {
            try
            {
                _ = ExcelConfigReader.GetEndpoint(key);
                continue;
            }
            catch
            {
                // add below
            }

            ExcelReader.AddRow(
                TestConfigDefaults.EndpointExcelFile,
                TestConfigDefaults.EndpointSheet,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [TestConfigDefaults.KeyColumn] = key,
                    [TestConfigDefaults.ValueColumn] = path
                });
        }
    }

    private static void EnsurePatientBodies()
    {
        var bodies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["patientGetFhirData_Body"] = """{"businessUnitIds":["67793c57-831f-11ea-bd7d-04d9f5ab62dc"],"currentBusinessUnitId":"67793c57-831f-11ea-bd7d-04d9f5ab62dc"}""",
            ["patientUpdate_Body"] = """{"firstName":"SecurityTest","lastName":"Patient"}""",
            ["patientFacesheet_Body"] = """{"includeAllSections":true}""",
            ["patientDelete_Body"] = """{"0":"1"}""",
            ["patientBulkProviders_Body"] = """["00000000-0000-0000-0000-000000000001"]""",
            ["patientEmrSearch_Body"] = """{"firstName":"Test","lastName":"Patient","dateOfBirth":"1990-01-01"}""",
            ["patientEmrAddUpdate_Body"] = """{"firstName":"Test","lastName":"Patient","dateOfBirth":"1990-01-01","gender":"Unknown"}"""
        };

        var filePath = FileUploadHelper.GetFilePath(TestConfigDefaults.BodyExcelFile);
        ExcelReader.WithWorkbook(filePath, workbook =>
        {
            foreach (var (sheetName, json) in bodies)
            {
                if (workbook.TryGetWorksheet(sheetName, out _))
                    continue;

                var sheet = workbook.AddWorksheet(sheetName);
                sheet.Cell(1, 1).Value = TestConfigDefaults.FieldColumn;
                sheet.Cell(1, 2).Value = TestConfigDefaults.ValueColumn;
                sheet.Cell(2, 1).Value = TestConfigDefaults.BodyRawJsonMarker;
                sheet.Cell(2, 2).Value = json;
            }
        }, save: true);
    }
}
