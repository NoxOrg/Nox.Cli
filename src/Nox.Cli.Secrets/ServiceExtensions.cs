using Microsoft.Extensions.DependencyInjection;

namespace Nox.Cli.Secrets;

public static class ServiceExtensions
{
    public static IServiceCollection AddProjectSecretResolver(this IServiceCollection services)
    {
        services.AddSingleton<IProjectSecretResolver, ProjectSecretResolver>();
        return services;
    }
    
    public static IServiceCollection AddOrgSecretResolver(this IServiceCollection services)
    {
        services.AddSingleton<IOrgSecretResolver, OrgSecretResolver>();
        return services;
    }
    
    public static IServiceCollection AddServerSecretResolver(this IServiceCollection services)
    {
        services.AddSingleton<IServerSecretResolver, ServerSecretResolver>();
        return services;
    }
}