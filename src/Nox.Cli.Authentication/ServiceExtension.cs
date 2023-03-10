using Microsoft.Extensions.DependencyInjection;

namespace Nox.Cli.Authentication;

public static class ServiceExtension
{
    public static IServiceCollection AddNoxServerAuthentication(this IServiceCollection services)
    {
        services.AddDataProtection();
        services.AddSingleton<PersistedServerToken>();
        return services;
    }
}