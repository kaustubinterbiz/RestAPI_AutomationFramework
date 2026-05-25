using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Configurations;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Authentication;

/// <summary>
/// Ensures a single login per process (or per machine via file cache) for parallel runs.
/// </summary>
public static class SharedTokenProvider
{
    private static readonly SemaphoreSlim LoginGate = new(1, 1);

    private static string? _processToken;
    private static DateTimeOffset _processExpiresAtUtc;

    public static bool TryGetInMemoryToken(out string token)
    {
        if (IsValid(_processToken, _processExpiresAtUtc, AppConfiguration.Authentication.RefreshBufferMinutes))
        {
            token = _processToken!;
            return true;
        }

        token = string.Empty;
        return false;
    }

    public static async Task<RestResponse> LoginAndStoreTokenAsync(
        ApiClient apiClient,
        Func<ApiClient, Task<(string? Token, DateTimeOffset ExpiresAtUtc, RestResponse Response)>> loginFunc,
        bool forceRefresh = false)
    {
        var settings = AppConfiguration.Authentication;

        if (settings.Mode == AuthenticationMode.PerScenario || forceRefresh)
        {
            var (token, expiresAt, response) = await loginFunc(apiClient);
            if (!string.IsNullOrWhiteSpace(token))
            {
                StoreProcessToken(token, expiresAt);
                PersistToken(token, expiresAt, settings);
            }

            return response;
        }

        await LoginGate.WaitAsync();
        try
        {
            if (TryGetInMemoryToken(out var existing))
            {
                TokenManager.ApplyToCurrentScenario(existing);
                return CreateSyntheticSuccessResponse();
            }

            if (TryLoadCachedToken(settings, out var cachedToken, out var cachedExpiry))
            {
                StoreProcessToken(cachedToken, cachedExpiry);
                TokenManager.SetAccessToken(cachedToken);
                return CreateSyntheticSuccessResponse();
            }

            var (newToken, expiresAtUtc, loginResponse) = await loginFunc(apiClient);
            if (string.IsNullOrWhiteSpace(newToken))
            {
                return loginResponse;
            }

            StoreProcessToken(newToken, expiresAtUtc);
            PersistToken(newToken, expiresAtUtc, settings);
            return loginResponse;
        }
        finally
        {
            LoginGate.Release();
        }
    }

    public static async Task EnsureAuthenticatedAsync(
        ApiClient apiClient,
        Func<ApiClient, Task<(string? Token, DateTimeOffset ExpiresAtUtc, RestResponse Response)>> loginFunc)
    {
        if (TokenManager.HasToken && TryGetInMemoryToken(out _))
        {
            return;
        }

        if (TokenManager.HasToken && !TryGetInMemoryToken(out _))
        {
            TokenManager.ClearScenarioToken();
        }

        var response = await LoginAndStoreTokenAsync(apiClient, loginFunc);
        if (!TokenManager.HasToken)
        {
            throw new InvalidOperationException(
                $"Authentication failed. Status={(int)response.StatusCode}, Body={response.Content}");
        }
    }

    public static void ApplySharedTokenToScenario()
    {
        if (TryGetInMemoryToken(out var token))
        {
            TokenManager.ApplyToCurrentScenario(token);
        }
    }

    public static void InvalidateAllCaches()
    {
        _processToken = null;
        _processExpiresAtUtc = default;
        TokenManager.ClearScenarioToken();
    }

    /// <summary>Applies an expired/invalid token and marks in-memory cache as expired (for refresh tests).</summary>
    public static void ApplyExpiredTokenForTesting(string expiredToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expiredToken);
        InvalidateAllCaches();
        _processToken = expiredToken;
        _processExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(-1);
        TokenManager.SetAccessToken(expiredToken, persistToConfig: false);
    }

    private static bool TryLoadCachedToken(
        AuthenticationSettings settings,
        out string token,
        out DateTimeOffset expiresAtUtc)
    {
        token = string.Empty;
        expiresAtUtc = default;

        if (settings.UseCachedAccessToken)
        {
            var fromAppSettings = ConfigReaderNew.GetJsonValue("appsettings.json", "access_token");
            if (!string.IsNullOrWhiteSpace(fromAppSettings))
            {
                var expiry = JwtTokenHelper.GetExpiryUtc(fromAppSettings)
                    ?? DateTimeOffset.UtcNow.AddHours(1);
                if (IsValid(fromAppSettings, expiry, settings.RefreshBufferMinutes))
                {
                    token = fromAppSettings;
                    expiresAtUtc = expiry;
                    return true;
                }
            }
        }

        if (settings.Mode != AuthenticationMode.SharedAcrossProcesses)
        {
            return false;
        }

        var roleKey = AppConfiguration.GetValue("LoginRoleKey");
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            roleKey = "OrganizationRole";
        }

        if (TokenCacheStore.TryLoadValid(
                settings.TokenCacheFile,
                roleKey,
                AppConfiguration.EnvironmentName,
                settings.RefreshBufferMinutes,
                out var entry))
        {
            token = entry.AccessToken;
            expiresAtUtc = entry.ExpiresAtUtc;
            return true;
        }

        return false;
    }

    private static void PersistToken(
        string token,
        DateTimeOffset expiresAtUtc,
        AuthenticationSettings settings)
    {
        TokenManager.SetAccessToken(token);

        if (settings.Mode != AuthenticationMode.SharedAcrossProcesses)
        {
            return;
        }

        var roleKey = AppConfiguration.GetValue("LoginRoleKey");
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            roleKey = "OrganizationRole";
        }

        TokenCacheStore.Save(
            settings.TokenCacheFile,
            new CachedTokenEntry
            {
                AccessToken = token,
                ExpiresAtUtc = expiresAtUtc,
                LoginRoleKey = roleKey,
                Environment = AppConfiguration.EnvironmentName
            });
    }

    private static void StoreProcessToken(string token, DateTimeOffset expiresAtUtc)
    {
        _processToken = token;
        _processExpiresAtUtc = expiresAtUtc;
        TokenManager.SetAccessToken(token, persistToConfig: false);
    }

    private static bool IsValid(string? token, DateTimeOffset expiresAtUtc, int refreshBufferMinutes) =>
        !string.IsNullOrWhiteSpace(token)
        && expiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(refreshBufferMinutes);

    private static RestResponse CreateSyntheticSuccessResponse() =>
        new() { ResponseStatus = ResponseStatus.Completed, StatusCode = System.Net.HttpStatusCode.OK };
}
