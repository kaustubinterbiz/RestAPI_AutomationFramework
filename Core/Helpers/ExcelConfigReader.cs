using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// Reads test configuration from Excel workbooks (Phase 1+: RequestEndPoint, LoginRequest, RequestBody).
/// Falls back to JSON files referenced in appsettings when Excel is unavailable.
/// </summary>
public static class ExcelConfigReader
{
    private const string AppSettingsFile = "appsettings.json";
    private const string EndpointJsonKey = "EndpointJson";
    private const string LoginJsonKey = "LoginJson";
    private const string BodyJsonKey = "JsonBody";

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        PropertyNamingPolicy = null
    };

    public static string GetEndpoint(string endpointKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointKey);

        ExcelConfigBootstrap.EnsureRequestEndPointWorkbook();

        if (TryGetKeyValue(
                TestConfigDefaults.EndpointExcelFile,
                TestConfigDefaults.EndpointSheet,
                endpointKey,
                out var endpoint))
        {
            return endpoint;
        }

        return GetEndpointFromJsonFallback(endpointKey);
    }

    public static string GetLoginCredentialsJson(string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        ExcelConfigBootstrap.EnsureLoginRequestWorkbook();

        if (TryGetLoginRoleRow(role, out var row))
        {
            var credentials = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in TestConfigDefaults.LoginCredentialColumns)
            {
                if (row.TryGetValue(field, out var value) && !string.IsNullOrWhiteSpace(value))
                    credentials[field] = value;
            }

            if (credentials.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Role '{role}' was found in '{TestConfigDefaults.LoginExcelFile}' but has no credential fields.");
            }

            return JsonSerializer.Serialize(credentials, JsonWriteOptions);
        }

        return GetLoginCredentialsFromJsonFallback(role);
    }

    public static string GetRequestBodyJson(string bodyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bodyKey);

        ExcelConfigBootstrap.EnsureRequestBodyWorkbook();

        if (TryBuildBodyJsonFromSheet(bodyKey, out var bodyJson))
            return bodyJson!;

        return GetRequestBodyFromJsonFallback(bodyKey);
    }

    public static string? GetEndpointResponseValue(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        ExcelConfigBootstrap.EnsureRequestEndPointWorkbook();

        return TryGetKeyValue(
            TestConfigDefaults.EndpointExcelFile,
            TestConfigDefaults.EndpointResponseSheet,
            key,
            out var value)
            ? value
            : null;
    }

    public static bool TryGetKeyValue(
        string fileName,
        string sheetName,
        string key,
        out string value)
    {
        value = string.Empty;

        if (!WorkbookExists(fileName))
            return false;

        var rows = ExcelReader.ReadSheet(fileName, sheetName);
        var match = rows.FirstOrDefault(r =>
            r.TryGetValue(TestConfigDefaults.KeyColumn, out var k)
            && string.Equals(k.Trim(), key, StringComparison.OrdinalIgnoreCase));

        if (match == null
            || !match.TryGetValue(TestConfigDefaults.ValueColumn, out var rawValue)
            || string.IsNullOrWhiteSpace(rawValue))
        {
            value = string.Empty;
            return false;
        }

        value = rawValue;
        return true;
    }

    public static bool TryGetLoginRoleRow(string role, out Dictionary<string, string> row)
    {
        row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!WorkbookExists(TestConfigDefaults.LoginExcelFile))
            return false;

        var rows = ExcelReader.ReadSheet(
            TestConfigDefaults.LoginExcelFile,
            TestConfigDefaults.LoginRolesSheet);

        var match = rows.FirstOrDefault(r =>
            r.TryGetValue(TestConfigDefaults.RoleColumn, out var roleName)
            && string.Equals(roleName.Trim(), role, StringComparison.OrdinalIgnoreCase));

        if (match == null)
            return false;

        row = match;
        return true;
    }

    private static bool TryBuildBodyJsonFromSheet(string bodyKey, out string? bodyJson)
    {
        bodyJson = null;

        if (!WorkbookExists(TestConfigDefaults.BodyExcelFile))
            return false;

        var sheetNames = ExcelReader.GetSheetNames(TestConfigDefaults.BodyExcelFile);
        var sheetName = sheetNames.FirstOrDefault(name =>
            string.Equals(name, bodyKey, StringComparison.OrdinalIgnoreCase));

        if (sheetName == null)
            return false;

        var rows = ExcelReader.ReadSheet(TestConfigDefaults.BodyExcelFile, sheetName);
        if (rows.Count == 0)
            return false;

        var rawJsonRow = rows.FirstOrDefault(row =>
            row.TryGetValue(TestConfigDefaults.FieldColumn, out var field)
            && string.Equals(field.Trim(), TestConfigDefaults.BodyRawJsonMarker, StringComparison.OrdinalIgnoreCase)
            && row.TryGetValue(TestConfigDefaults.ValueColumn, out var raw)
            && !string.IsNullOrWhiteSpace(raw));

        if (rawJsonRow != null)
        {
            bodyJson = rawJsonRow[TestConfigDefaults.ValueColumn];
            return true;
        }

        var bodyObject = new JsonObject();
        foreach (var row in rows)
        {
            if (!row.TryGetValue(TestConfigDefaults.FieldColumn, out var fieldName)
                || string.IsNullOrWhiteSpace(fieldName)
                || !row.TryGetValue(TestConfigDefaults.ValueColumn, out var fieldValue))
            {
                continue;
            }

            bodyObject[fieldName.Trim()] = ParseBodyFieldValue(fieldValue);
        }

        if (bodyObject.Count == 0)
            return false;

        bodyJson = bodyObject.ToJsonString(JsonWriteOptions);
        return true;
    }

    private static JsonNode? ParseBodyFieldValue(string rawValue)
    {
        if (string.Equals(rawValue, "true", StringComparison.OrdinalIgnoreCase))
            return JsonValue.Create(true);

        if (string.Equals(rawValue, "false", StringComparison.OrdinalIgnoreCase))
            return JsonValue.Create(false);

        if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
            return JsonValue.Create(longValue);

        if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
            return JsonValue.Create(doubleValue);

        return JsonValue.Create(rawValue);
    }

    private static bool WorkbookExists(string fileName)
    {
        try
        {
            _ = FileUploadHelper.GetFilePath(fileName);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    private static string GetEndpointFromJsonFallback(string endpointKey)
    {
        ConfigReaderNew.LoadConfig(AppSettingsFile);
        var endpointJsonPath = ConfigReaderNew.GetValue(EndpointJsonKey);
        if (string.IsNullOrWhiteSpace(endpointJsonPath))
        {
            throw new InvalidOperationException(
                $"Endpoint key '{endpointKey}' was not found in Excel and '{EndpointJsonKey}' is not configured.");
        }

        ConfigReaderNew.LoadConfig(endpointJsonPath);
        var value = ConfigReaderNew.GetValue(endpointKey);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Endpoint key '{endpointKey}' was not found in '{TestConfigDefaults.EndpointExcelFile}' or '{endpointJsonPath}'.");
        }

        return value;
    }

    private static string GetLoginCredentialsFromJsonFallback(string role)
    {
        ConfigReaderNew.LoadConfig(AppSettingsFile);
        var loginJsonPath = ConfigReaderNew.GetValue(LoginJsonKey);
        if (string.IsNullOrWhiteSpace(loginJsonPath))
        {
            throw new InvalidOperationException(
                $"Login role '{role}' was not found in Excel and '{LoginJsonKey}' is not configured.");
        }

        return ConfigReaderNew.GetJsonBody(loginJsonPath, role);
    }

    private static string GetRequestBodyFromJsonFallback(string bodyKey)
    {
        ConfigReaderNew.LoadConfig(AppSettingsFile);
        var bodyJsonPath = ConfigReaderNew.GetValue(BodyJsonKey);
        if (string.IsNullOrWhiteSpace(bodyJsonPath))
        {
            throw new InvalidOperationException(
                $"Body key '{bodyKey}' was not found in Excel and '{BodyJsonKey}' is not configured.");
        }

        return ConfigReaderNew.GetJsonBody(bodyJsonPath, bodyKey);
    }
}
