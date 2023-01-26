using Microsoft.Extensions.DependencyInjection;

namespace Nox.Cli.Server.Integration;

public static class ServiceExtension
{
    public static IServiceCollection AddNoxCliServerIntegration(this IServiceCollection services, string serverUrl)
    {
        services.AddSingleton<INoxCliServerIntegration>(impl => new NoxCliServerIntegration(serverUrl)); 
        return services;
    }
    
}