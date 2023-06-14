using Microsoft.Extensions.DependencyInjection;
using Nox.Cli.Server.Abstractions;

namespace Nox.Cli.Server.Cache;

public static class ServiceExtension
{
    public static IServiceCollection AddWorkflowCache(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IServerCache, ServerCache>();
        return services;
    } 
}