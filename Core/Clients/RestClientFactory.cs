using System.Collections.Concurrent;
using EnterpriseApiAutomationFramework.Core.Configurations;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Clients;

/// <summary>
/// Creates and caches one RestSharp client per <see cref="ApiHost"/>.
/// Thread-safe for parallel SpecFlow scenarios.
/// </summary>
public sealed class RestClientFactory
{
    private readonly ApiEnvironmentSettings _settings;
    private readonly ConcurrentDictionary<ApiHost, RestClient> _clients = new();

    public RestClientFactory(ApiEnvironmentSettings? settings = null)
    {
        _settings = settings ?? AppConfiguration.ApiUrls;
    }

    public RestClient GetClient(ApiHost host) =>
        _clients.GetOrAdd(host, CreateClient);

    private RestClient CreateClient(ApiHost host)
    {
        var baseUrl = host switch
        {
            ApiHost.Auth => _settings.AuthBaseUrl,
            ApiHost.Api => _settings.ApiBaseUrl,
            _ => throw new ArgumentOutOfRangeException(nameof(host), host, "Unknown API host.")
        };

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                $"Base URL for {host} is not configured. Set ApiUrls in appsettings (AuthBaseUrl / ApiBaseUrl).");
        }

        var normalized = baseUrl.TrimEnd('/') + "/";
        var options = new RestClientOptions(normalized)
        {
            Timeout = TimeSpan.FromMilliseconds(_settings.TimeoutMilliseconds)
        };

        return new RestClient(options);
    }
}
