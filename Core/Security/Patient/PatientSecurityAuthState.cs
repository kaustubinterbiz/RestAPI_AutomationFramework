namespace EnterpriseApiAutomationFramework.Core.Security.Patient;

public sealed class PatientSecurityAuthState
{
    public bool SendAuthorizationHeader { get; init; } = true;
    public string? BearerToken { get; init; }
    public bool BearerTokenProvided { get; init; }
}
