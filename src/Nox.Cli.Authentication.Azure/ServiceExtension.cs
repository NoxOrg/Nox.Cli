using Microsoft.Extensions.DependencyInjection;
using Nox.Cli.Caching;

namespace Nox.Cli.Authentication.Azure;

public static class ServiceExtension
{
    public static IServiceCollection AddAzureAuthentication(this IServiceCollection services)
    {
        services.AddDataProtection();
        services.AddSingleton<IAuthenticator, AzureAuthenticator>();
        return services;
    }
}