using Microsoft.Extensions.DependencyInjection;

namespace Nox.Cli.Server.Integration;

public static class ServiceExtension
{
    public static IServiceCollection AddServerIntegration(this IServiceCollection services)
    {
        services.AddSingleton<INoxCliServerIntegration, NoxCliServerIntegration>();
        return services;
    }
}