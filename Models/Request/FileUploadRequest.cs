  namespace EnterpriseApiAutomationFramework.Models.Request;

public class FileUploadRequest
{
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Multipart form field name that maps to the file (e.g. "file", "document").</summary>
    public string FileParameterName { get; set; } = "file";

    public string FilePath { get; set; } = string.Empty;

    public Dictionary<string, string>? FormFields { get; set; }

    public Dictionary<string, string>? Headers { get; set; }
}
