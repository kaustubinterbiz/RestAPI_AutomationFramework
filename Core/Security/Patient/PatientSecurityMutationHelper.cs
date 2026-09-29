using System.Text.Json;
using System.Text.Json.Nodes;
using EnterpriseApiAutomationFramework.Core.Builders;
using EnterpriseApiAutomationFramework.Core.Helpers;

namespace EnterpriseApiAutomationFramework.Core.Security.Patient;

/// <summary>
/// Applies IDOR / Input Validation mutations from matrix tampered-request hints.
/// </summary>
public static class PatientSecurityMutationHelper
{
    public static Dictionary<string, string>? BuildIdorSegmentOverrides(PatientSecurityEntry entry)
    {
        var otherPatient = PatientSecurityTestDataConfig.OrgBPatientId;
        var otherBu = PatientSecurityTestDataConfig.OrgBBusinessUnitId;
        var randomGuid = PatientSecurityTestDataConfig.RandomGuid;

        if (string.IsNullOrWhiteSpace(otherPatient) && string.IsNullOrWhiteSpace(otherBu))
            return null;

        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        ApplyOverridesForKeys(entry.UrlSegmentKeys, overrides, otherPatient, otherBu, randomGuid);
        ApplyOverridesForKeys(entry.QueryParamKeys, overrides, otherPatient, otherBu, randomGuid);

        return overrides.Count > 0 ? overrides : null;
    }

    public static string? BuildValidationBodyOverride(PatientSecurityEntry entry)
    {
        var baseline = RequestBuilder.ResolveBodyFromConfig(entry.BodyKey, "appsettings.json");
        var mutation = entry.MutationApplied ?? entry.TamperedRequest ?? string.Empty;
        var lower = mutation.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(baseline))
        {
            if (lower.Contains("empty body") || lower.Contains("null body"))
                return "{}";
            if (lower.Contains("invalid guid"))
                return $"{{\"patientId\":\"{PatientSecurityTestDataConfig.RandomGuid}\"}}";
            return null;
        }

        try
        {
            var node = JsonNode.Parse(baseline);
            if (node == null)
                return baseline;

            if (lower.Contains("mass assign") || lower.Contains("mass-assign") || lower.Contains("extra field"))
            {
                if (node is JsonObject obj)
                {
                    obj["isAdmin"] = true;
                    obj["role"] = "SuperAdmin";
                    obj["businessUnitId"] = PatientSecurityTestDataConfig.OrgBBusinessUnitId;
                }
            }

            if (lower.Contains("invalid guid"))
            {
                ReplaceGuidFields(node, PatientSecurityTestDataConfig.RandomGuid);
            }

            if (lower.Contains("negative") || lower.Contains("huge"))
            {
                ReplaceNumericFields(node, -999999);
            }

            if (lower.Contains("sqli") || lower.Contains("xss") || lower.Contains("injection"))
            {
                InjectStringFields(node, "' OR 1=1 --");
            }

            if (lower.Contains("bulk") || lower.Contains("array") || lower.Contains("100"))
            {
                if (node is JsonArray)
                    return BuildBulkPatientArray(100);
            }

            return node.ToJsonString();
        }
        catch
        {
            return baseline;
        }
    }

    private static void ApplyOverridesForKeys(
        string? keysSpec,
        Dictionary<string, string> overrides,
        string otherPatient,
        string otherBu,
        string randomGuid)
    {
        if (string.IsNullOrWhiteSpace(keysSpec))
            return;

        foreach (var item in keysSpec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var mapKey = item.Contains('=') ? item.Split('=', 2)[0].Trim() : item.Trim();

            if (mapKey.Contains("patient", StringComparison.OrdinalIgnoreCase))
                overrides[mapKey] = string.IsNullOrWhiteSpace(otherPatient) ? randomGuid : otherPatient;
            else if (mapKey.Contains("business", StringComparison.OrdinalIgnoreCase))
                overrides[mapKey] = string.IsNullOrWhiteSpace(otherBu) ? randomGuid : otherBu;
            else if (mapKey.Contains("dataType", StringComparison.OrdinalIgnoreCase))
                overrides[mapKey] = PatientSecurityTestDataConfig.DataType;
            else if (mapKey.Contains("service", StringComparison.OrdinalIgnoreCase))
                overrides[mapKey] = PatientSecurityTestDataConfig.ServiceRequestId;
        }
    }

    private static string BuildBulkPatientArray(int count)
    {
        var ids = new JsonArray();
        var other = PatientSecurityTestDataConfig.OrgBPatientId;
        var own = PatientSecurityTestDataConfig.OrgAPatientId;
        for (var i = 0; i < count; i++)
            ids.Add(i % 2 == 0 ? own : other);
        return ids.ToJsonString();
    }

    private static void ReplaceGuidFields(JsonNode node, string value)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(p => p.Key).ToList())
            {
                if (key.Contains("id", StringComparison.OrdinalIgnoreCase)
                    && obj[key]?.GetValueKind() == JsonValueKind.String)
                {
                    obj[key] = value;
                }
                else if (obj[key] != null)
                {
                    ReplaceGuidFields(obj[key]!, value);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item != null)
                    ReplaceGuidFields(item, value);
            }
        }
    }

    private static void ReplaceNumericFields(JsonNode node, int value)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(p => p.Key).ToList())
            {
                if (obj[key]?.GetValueKind() is JsonValueKind.Number)
                    obj[key] = value;
                else if (obj[key] != null)
                    ReplaceNumericFields(obj[key]!, value);
            }
        }
    }

    private static void InjectStringFields(JsonNode node, string payload)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(p => p.Key).ToList())
            {
                if (obj[key]?.GetValueKind() == JsonValueKind.String)
                    obj[key] = payload;
                else if (obj[key] != null)
                    InjectStringFields(obj[key]!, payload);
            }
        }
    }
}
