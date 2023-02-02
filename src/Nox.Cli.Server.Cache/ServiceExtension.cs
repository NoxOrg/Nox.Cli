using Microsoft.Extensions.DependencyInjection;

namespace Nox.Cli.Server.Cache;

public static class ServiceExtension
{
    public static IServiceCollection AddWorkflowCache(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IWorkflowCache, WorkflowCache>();
        return services;
    } 
}