using Microsoft.Extensions.DependencyInjection;
using Nox.Cli.Abstractions.Secrets;
using Nox.Secrets.Abstractions;

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
        services.AddSingleton<IServerSecretResolver>(sp => new ServerSecretResolver(sp.GetRequiredService<IPersistedSecretStoreEx>(), tenantId, clientId, clientSecret));
        return services;
    }
    
    public static IServiceCollection AddPersistedSecretStore(this IServiceCollection services)
    {
        services.AddDataProtection();
        services.AddSingleton<IPersistedSecretStore, Nox.Secrets.PersistedSecretStore>();
        services.AddSingleton<IPersistedSecretStoreEx, PersistedSecretStore>();
        return services;
    }
}