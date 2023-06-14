using Microsoft.Extensions.DependencyInjection;
using Nox.Cli.Abstractions.Caching;

namespace Nox.Cli.Caching;

public static class ServiceExtensions
{
    public static IServiceCollection AddNoxCliCacheManager(this IServiceCollection services, INoxCliCacheManager instance)
    {
        services.AddSingleton<INoxCliCacheManager>(instance);
        return services;
    }

    public static IServiceCollection AddNoxTokenCache(this IServiceCollection services)
    {
        services.AddDataProtection();
        services.AddSingleton<IPersistedTokenCache, PersistedTokenCache>();
        return services;
    }
}