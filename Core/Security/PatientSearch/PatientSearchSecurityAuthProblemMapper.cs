namespace EnterpriseApiAutomationFramework.Core.Security.PatientSearch;

public static class PatientSearchSecurityAuthProblemMapper
{
    public static string MapToAuthVariant(string authProblemLabel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authProblemLabel);

        var key = authProblemLabel.Trim().ToLowerInvariant();
        return key switch
        {
            "missing authorization header" => "NoAuthHeader",
            "empty bearer token" => "EmptyBearer",
            "invalid token" => "InvalidToken",
            "malformed token" => "MalformedToken",
            "expired token" => "ExpiredToken",
            "tampered token" => "TamperedSignature",
            "bearer prefix missing" => "MissingBearerPrefix",
            _ => throw new ArgumentException(
                $"Unknown auth problem '{authProblemLabel}'. Use labels from the feature Examples table.")
        };
    }
}
