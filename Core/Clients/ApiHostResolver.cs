namespace EnterpriseApiAutomationFramework.Core.Clients;

/// <summary>
/// Maps feature-file base URL type keys to <see cref="ApiHost"/>.
/// Use tags @Auth / @Api or step: Given the base url type is "Auth"
/// </summary>
public static class ApiHostResolver
{
    private static readonly HashSet<string> AuthKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Auth", "B2C", "Login", "Token", "Identity", "AuthBaseUrl"
    };

    private static readonly HashSet<string> ApiKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Api", "App", "Application", "ApiBaseUrl", "Rovicare"
    };

    public static bool TryResolveFromKey(string? baseUrlTypeKey, out ApiHost host)
    {
        host = ApiHost.Api;

        if (string.IsNullOrWhiteSpace(baseUrlTypeKey))
        {
            return false;
        }

        var key = baseUrlTypeKey.Trim();

        if (AuthKeys.Contains(key))
        {
            host = ApiHost.Auth;
            return true;
        }

        if (ApiKeys.Contains(key))
        {
            host = ApiHost.Api;
            return true;
        }

        return false;
    }

    public static ApiHost ResolveFromKey(string baseUrlTypeKey)
    {
        if (TryResolveFromKey(baseUrlTypeKey, out var host))
        {
            return host;
        }

        throw new ArgumentException(
            $"Unknown base URL type '{baseUrlTypeKey}'. Use Auth or Api (aliases: B2C, Login, App, Application).");
    }

    public static ApiHost? ResolveFromTags(IEnumerable<string> tags)
    {
        ApiHost? resolved = null;

        foreach (var tag in tags)
        {
            if (!TryResolveFromKey(tag, out var host))
            {
                continue;
            }

            if (resolved.HasValue && resolved.Value != host)
            {
                throw new InvalidOperationException(
                    $"Conflicting base URL tags on feature/scenario: found both {resolved.Value} and {host}.");
            }

            resolved = host;
        }

        return resolved;
    }
}
