using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;

namespace Nox.Cli.Authentication;

public static class ServiceExtension
{
    public static IServiceCollection AddNoxServerAuthentication(this IServiceCollection services)
    {
        services.AddSingleton<IAuthenticator, Authenticator>();
        
        return services;
    }
}