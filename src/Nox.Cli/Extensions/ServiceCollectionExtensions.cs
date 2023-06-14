using Nox.Core.Extensions;
using Nox.Core.Helpers;
using Nox.Core.Interfaces;
using Nox.Core.Models;
using Spectre.Console;

namespace Nox.Cli;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nox.Core.Constants;
using Nox.Core.Exceptions;
using System.IO;

public static class ServiceCollectionExtensions
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

        if (Directory.GetFiles(designPath, FileExtension.ServiceDefinition, SearchOption.AllDirectories).Length > 0)
        {
            services.AddNoxConfiguration(designPath);
            AnsiConsole.MarkupLine($"Found solution configuration in {Path.GetFullPath(designPath)}");
        }
        else
        {
            services.AddSingleton<IProjectConfiguration>(new ProjectConfiguration());
        }
        return services;
    }

    private static string ResolveDesignPath(string[] args, IConfiguration configuration)
    {
        string? path = null;
        
        for (var i = args.Length-1; i >= 0; i--)
        {
            if (args[i].Equals("--path", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    path = args[i + 1];
                }
            }
        }

        path ??= configuration["Nox:DefinitionRootPath"];

        path ??= Directory.GetCurrentDirectory();

        return path;
    }
}