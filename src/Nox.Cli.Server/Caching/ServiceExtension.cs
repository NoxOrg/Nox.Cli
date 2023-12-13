using Nox.Cli.Server.Abstractions;

namespace Nox.Cli.Server.Caching;

public static class ServiceExtension
{
    public static IServiceCollection AddWorkflowCache(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IServerCache, ServerCache>();
        return services;
    } 
}