using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Models.Request;

namespace EnterpriseApiAutomationFramework.Core.Helpers;


public static class FileUploadHelper
{
    public static string GetFilePath(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var relativePath = Path.Combine("TestData", "UploadFiles", fileName);
        return ConfigReaderNew.ResolvePathForRead(relativePath);
    }

    /// <summary>
    /// Builds a FileUploadRequest by resolving the file path automatically.
    /// </summary>
    /// <param name="endpointKey">Key from RequestEndPoint.json (e.g. "UploadDocument").</param>
    /// <param name="fileName">Filename only — e.g. "TestData.xlsx". Folder is auto-resolved.</param>
    /// <param name="fileParameterName">Multipart field name expected by the API (e.g. "file", "document").</param>
    /// <param name="formFields">Additional form fields sent alongside the file.</param>
    /// <param name="headers">Extra request headers (Authorization is auto-added by ApiClient).</param>
    public static FileUploadRequest Build(
        string endpointKey,
        string fileName,
        string fileParameterName = "file",
        Dictionary<string, string>? formFields = null,
        Dictionary<string, string>? headers = null)
    {
        return new FileUploadRequest
        {
            Endpoint          = endpointKey,
            FileParameterName = fileParameterName,
            FilePath          = GetFilePath(fileName),
            FormFields        = formFields,
            Headers           = headers
        };
    }
}
