using EnterpriseApiAutomationFramework.Core.Authentication;
using EnterpriseApiAutomationFramework.Core.Authorization;
using EnterpriseApiAutomationFramework.Core.Configurations;
using EnterpriseApiAutomationFramework.Drivers;
using EnterpriseApiAutomationFramework.StepDefinitions;
using Reqnroll;

namespace EnterpriseApiAutomationFramework.Core.Security.Patient;

public sealed class PatientSecurityAuthHelper
{
    private readonly UserDriver _driver;
    private readonly Dictionary<string, string> _baselineTokensByRole =
        new(StringComparer.OrdinalIgnoreCase);

    public PatientSecurityAuthHelper(UserDriver? driver = null) =>
        _driver = driver ?? new UserDriver();

    public async Task<string> EnsureBaselineTokenAsync(ScenarioContext context, string role)
    {
        if (_baselineTokensByRole.TryGetValue(role, out var cached) && !string.IsNullOrWhiteSpace(cached))
            return cached;

        RoleProvider.ValidateRoleExists(role);
        SharedTokenProvider.InvalidateAllCaches();
        TokenManager.ClearScenarioToken();

        ApiHostStepHelper.ApplyBaseUrlType("Auth");
        var loginResponse = await _driver.LoginAsync(role);
        if (!loginResponse.IsSuccessful)
        {
            throw new InvalidOperationException(
                $"Baseline login failed for role '{role}'. Status={(int)loginResponse.StatusCode}");
        }

        ApiAuth.SaveTokenFromLoginResponse(context, loginResponse.Content);
        var token = TokenManager.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException($"Baseline login for '{role}' did not produce access_token.");

        _baselineTokensByRole[role] = token;
        return token;
    }

    public async Task PrepareSessionAsync(ScenarioContext context, string role)
    {
        await EnsureBaselineTokenAsync(context, role);
        ApiHostStepHelper.ApplyFeatureName(AuthorizationConstants.SessionFeatureName);
        var sessionResponse = await _driver.GetAsync();
        StoreInfo.SaveSessionInfoFromResponse(sessionResponse.Content);
    }

    public PatientSecurityAuthState BuildAuthState(string scenarioType, string? validToken)
    {
        switch (scenarioType.ToUpperInvariant())
        {
            case "BASELINE":
            case "IDORCROSSORG":
            case "INPUTVALIDATION":
                return BuildBearerState(validToken);

            case "NOAUTHHEADER":
                return new PatientSecurityAuthState { SendAuthorizationHeader = false };

            case "EMPTYBEARER":
                return new PatientSecurityAuthState
                {
                    SendAuthorizationHeader = true,
                    BearerTokenProvided = true,
                    BearerToken = string.Empty
                };

            case "INVALIDTOKEN":
                return BuildBearerState(TokenTestHelper.GetGarbageAccessToken());

            case "MALFORMEDTOKEN":
                return BuildBearerState(TokenTestHelper.GetMalformedAccessToken());

            case "EXPIREDTOKEN":
                if (string.IsNullOrWhiteSpace(validToken))
                    throw new InvalidOperationException("ExpiredToken requires baseline JWT.");
                return BuildBearerState(TokenTestHelper.GetStructurallyExpiredAccessToken(validToken));

            case "TAMPEREDSIGNATURE":
                if (string.IsNullOrWhiteSpace(validToken))
                    throw new InvalidOperationException("TamperedSignature requires baseline JWT.");
                return BuildBearerState(TokenTestHelper.GetSignatureTamperedAccessToken(validToken));

            case "WRONGISSUERAUDIENCE":
                if (string.IsNullOrWhiteSpace(validToken))
                    throw new InvalidOperationException("WrongIssuerAudience requires baseline JWT.");
                return BuildBearerState(TokenTestHelper.GetWrongIssuerAudienceToken(validToken));

            case "MISSINGCLAIM":
                if (string.IsNullOrWhiteSpace(validToken))
                    throw new InvalidOperationException("MissingClaim requires baseline JWT.");
                return BuildBearerState(TokenTestHelper.GetMissingClaimToken(validToken));

            default:
                throw new ArgumentException($"Unsupported Patient auth scenario '{scenarioType}'.");
        }
    }

    public static bool NeedsBaselineLogin(string scenarioType) =>
        scenarioType.ToUpperInvariant() switch
        {
            "NOAUTHHEADER" => false,
            "EMPTYBEARER" => false,
            "INVALIDTOKEN" => false,
            "MALFORMEDTOKEN" => false,
            _ => true
        };

    private static PatientSecurityAuthState BuildBearerState(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Valid bearer token required.");

        return new PatientSecurityAuthState
        {
            SendAuthorizationHeader = true,
            BearerTokenProvided = true,
            BearerToken = token
        };
    }
}
