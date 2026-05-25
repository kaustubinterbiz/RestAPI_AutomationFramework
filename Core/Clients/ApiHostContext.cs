namespace EnterpriseApiAutomationFramework.Core.Clients;

/// <summary>
/// Current scenario base URL host, set from feature tags or Given steps.
/// </summary>
public static class ApiHostContext
{
    private static readonly AsyncLocal<ApiHost?> Current = new();

    public static ApiHost CurrentOrDefault => Current.Value ?? ApiHost.Api;

    public static bool HasValue => Current.Value.HasValue;

    public static void Set(ApiHost host) => Current.Value = host;

    public static void Clear() => Current.Value = null;
}
