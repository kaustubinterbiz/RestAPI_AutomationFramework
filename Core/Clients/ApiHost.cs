namespace EnterpriseApiAutomationFramework.Core.Clients;

/// <summary>
/// Identifies which base URL an HTTP call should use.
/// Auth: Azure AD B2C token endpoint only.
/// Api: application APIs after login.
/// </summary>
public enum ApiHost
{
    Auth,
    Api
}
