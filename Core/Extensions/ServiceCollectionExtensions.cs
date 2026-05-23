using EnterpriseApiAutomationFramework.Core.Builders;
using EnterpriseApiAutomationFramework.Core.Clients;
using EnterpriseApiAutomationFramework.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseApiAutomationFramework.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterFrameworkServices(this IServiceCollection services)
    {
        services.AddSingleton<RestClientFactory>();
        services.AddTransient<ApiClient>();
        services.AddTransient<IRestBuilder, EnterpriseRestBuilder>();

        return services;
    }
}
