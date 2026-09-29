namespace EnterpriseApiAutomationFramework.Core.Security.Patient;

/// <summary>
/// PHI-safe response checks for Patient security scenarios.
/// </summary>
public static class PatientSecurityPhiAssert
{
    private static readonly string[] SensitivePatterns =
    [
        "ssn", "socialsecurity", "dateofbirth", "dob", "diagnosis", "mrn"
    ];

    public static bool ContainsForeignPatientData(string? responseBody, string foreignPatientId)
    {
        if (string.IsNullOrWhiteSpace(responseBody) || string.IsNullOrWhiteSpace(foreignPatientId))
            return false;

        return responseBody.Contains(foreignPatientId, StringComparison.OrdinalIgnoreCase);
    }

    public static string? ValidateNoSensitiveLeak(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return null;

        var lower = responseBody.ToLowerInvariant();
        foreach (var pattern in SensitivePatterns)
        {
            if (lower.Contains(pattern, StringComparison.Ordinal))
                return $"Response may contain sensitive field pattern '{pattern}'.";
        }

        return null;
    }
}
