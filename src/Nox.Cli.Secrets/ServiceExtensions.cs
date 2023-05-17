using Microsoft.Extensions.DependencyInjection;
using Nox.Utilities.Secrets;

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
    
    public static IServiceCollection AddServerSecretResolver(this IServiceCollection services, string tenantId, string clientId, string clientSecret)
    {
        services.AddSingleton<IServerSecretResolver>(sp => new ServerSecretResolver(sp.GetRequiredService<IPersistedSecretStore>(), tenantId, clientId, clientSecret));
        return services;
    }
}