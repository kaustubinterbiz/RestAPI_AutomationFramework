using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Security.Patient;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.Core.Security.PatientSearch;

public sealed class PatientSearchSecurityAuthHelper
{
    private readonly PatientSecurityAuthHelper _patientAuth = new();

    public async Task PrepareSessionAsync(ScenarioContext context, string role) =>
        await _patientAuth.PrepareSessionAsync(context, role);

    public PatientSearchSecurityAuthContext BuildAuthContext(string authVariant, string? baselineToken)
    {
        var variant = authVariant.Trim();

        if (string.Equals(variant, "MissingBearerPrefix", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(baselineToken))
                throw new InvalidOperationException("MissingBearerPrefix requires a baseline token.");

            return new PatientSearchSecurityAuthContext
            {
                AuthorizationHeaderValue = baselineToken,
                UseRawAuthorizationHeader = true
            };
        }

        var scenarioType = MapToPatientScenarioType(variant);
        var state = _patientAuth.BuildAuthState(scenarioType, baselineToken);
        if (!state.SendAuthorizationHeader)
        {
            return new PatientSearchSecurityAuthContext { SendAuthorizationHeader = false };
        }

        return new PatientSearchSecurityAuthContext
        {
            SendAuthorizationHeader = true,
            BearerToken = state.BearerToken,
            BearerTokenProvided = state.BearerTokenProvided
        };
    }

    public async Task<string?> ResolveBaselineTokenAsync(ScenarioContext context, string role)
    {
        if (PatientSecurityAuthHelper.NeedsBaselineLogin(MapToPatientScenarioType("Baseline")))
            await PrepareSessionAsync(context, role);

        return TokenManager.AccessToken;
    }

    private static string MapToPatientScenarioType(string authVariant) =>
        authVariant.ToUpperInvariant() switch
        {
            "NOAUTHHEADER" => PatientSecurityConstants.ScenarioNoAuthHeader,
            "EMPTYBEARER" => PatientSecurityConstants.ScenarioEmptyBearer,
            "INVALIDTOKEN" => PatientSecurityConstants.ScenarioInvalidToken,
            "MALFORMEDTOKEN" => PatientSecurityConstants.ScenarioMalformedToken,
            "EXPIREDTOKEN" => PatientSecurityConstants.ScenarioExpiredToken,
            "TAMPEREDSIGNATURE" => PatientSecurityConstants.ScenarioTamperedSignature,
            "BASELINE" => PatientSecurityConstants.ScenarioBaseline,
            _ => throw new ArgumentException($"Unsupported auth variant '{authVariant}'.")
        };
}

public sealed class PatientSearchSecurityAuthContext
{
    public bool SendAuthorizationHeader { get; init; } = true;
    public bool BearerTokenProvided { get; init; }
    public string? BearerToken { get; init; }
    public bool UseRawAuthorizationHeader { get; init; }
    public string? AuthorizationHeaderValue { get; init; }
}
