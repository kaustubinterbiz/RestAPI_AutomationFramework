namespace EnterpriseApiAutomationFramework.Core.Authentication;

/// <summary>
/// PerScenario: new login per scenario (isolated, slow in parallel).
/// Shared: one login per test process; all parallel scenarios reuse the token.
/// SharedAcrossProcesses: reuse token from file cache (for out-of-process parallel workers).
/// </summary>
public enum AuthenticationMode
{
    PerScenario,
    Shared,
    SharedAcrossProcesses
}
