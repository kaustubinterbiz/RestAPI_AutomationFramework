namespace EnterpriseApiAutomationFramework.Core.Security.Patient;

public static class PatientSecurityScenarioMapper
{
    public static string? MapScenarioType(string testCaseId, string sheetCategory, string? mutationApplied)
    {
        if (testCaseId.EndsWith("-01", StringComparison.OrdinalIgnoreCase)
            && string.Equals(sheetCategory, "Authentication", StringComparison.OrdinalIgnoreCase))
        {
            return PatientSecurityConstants.ScenarioBaseline;
        }

        if (string.Equals(sheetCategory, "IDOR", StringComparison.OrdinalIgnoreCase)
            || string.Equals(sheetCategory, "IdorCrossOrg", StringComparison.OrdinalIgnoreCase))
        {
            return PatientSecurityConstants.ScenarioIdorCrossOrg;
        }

        if (string.Equals(sheetCategory, "InputValidation", StringComparison.OrdinalIgnoreCase))
        {
            return PatientSecurityConstants.ScenarioInputValidation;
        }

        return MapAuthMutation(mutationApplied);
    }

    public static string? MapAuthMutation(string? mutationApplied)
    {
        if (string.IsNullOrWhiteSpace(mutationApplied))
            return null;

        var m = mutationApplied.ToLowerInvariant();

        if (m.Contains("remove authorization") || m.Contains("no token"))
            return PatientSecurityConstants.ScenarioNoAuthHeader;

        if (m.Contains("bearer (empty)") || m.Contains("empty bearer"))
            return PatientSecurityConstants.ScenarioEmptyBearer;

        if (m.Contains("garbage") || m.Contains("abc.def.ghi"))
            return PatientSecurityConstants.ScenarioInvalidToken;

        if (m.Contains("2-segment") || m.Contains("non-base64"))
            return PatientSecurityConstants.ScenarioMalformedToken;

        if (m.Contains("exp claim") || m.Contains("past its exp"))
            return PatientSecurityConstants.ScenarioExpiredToken;

        if (m.Contains("altered signature") || m.Contains("tampered signature"))
            return PatientSecurityConstants.ScenarioTamperedSignature;

        if (m.Contains("iss/aud") || m.Contains("another tenant"))
            return PatientSecurityConstants.ScenarioWrongIssuerAudience;

        if (m.Contains("without expected claim") || m.Contains("missing required claim"))
            return PatientSecurityConstants.ScenarioMissingClaim;

        return null;
    }
}
