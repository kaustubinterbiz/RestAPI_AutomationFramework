using EnterpriseApiAutomationFramework.Models.Request;

namespace EnterpriseApiAutomationFramework.Core.Helpers;


public static class FileUploadHelper
{
    private static readonly string UploadFilesFolder =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "UploadFiles");

   
    public static string GetFilePath(string fileName)
    {
        var fullPath = Path.Combine(UploadFilesFolder, fileName);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException(
                $"Upload file '{fileName}' not found. " +
                $"Place the file in: TestData/UploadFiles/  (resolved to: {fullPath})");

        return fullPath;
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
