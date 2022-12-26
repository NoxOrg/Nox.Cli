namespace Nox.Cli;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nox.Core.Configuration;
using Nox.Core.Exceptions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNoxCliServices(this IServiceCollection services, string[] args)
    {
        var configuration = ConfigurationHelper.GetNoxAppSettings();

        if (configuration == null)
        {
            throw new ConfigurationException("Could not load Nox configuration.");
        }

        var designPath = ResolveDesignPath(args, configuration);

        configuration["NoxCli:DesignFolder"] = designPath;

        services.AddSingleton(configuration);

        services.AddNoxConfiguration(designPath);

        return services;
    }

    private static string ResolveDesignPath(string[] args, IConfiguration configuration)
    {
        for (var i = args.Length-1; i >= 0; i--)
        {
            if (args[i].Equals("--path", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }
        }

        if (configuration["Nox:DefinitionRootPath"] is not null)
        {
            return configuration["Nox:DefinitionRootPath"]!;
        }

        return Directory.GetCurrentDirectory(); ;
    }
}