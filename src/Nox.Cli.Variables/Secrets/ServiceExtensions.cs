using Microsoft.Extensions.DependencyInjection;

namespace Nox.Cli.Variables.Secrets;

public static class ServiceExtensions
{
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
    
    public static IServiceCollection AddPersistedSecretStore(this IServiceCollection services)
    {
        services.AddDataProtection();
        services.AddSingleton<IPersistedSecretStore, PersistedSecretStore>();
        return services;
    }
}