using Nox.Cli.Commands;
using Spectre.Console.Cli;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Authentication;
using Nox.Cli.Authentication.Azure;
using Nox.Cli.Caching;
using Nox.Cli.Configuration.Validation;
using Nox.Cli.Server.Integration;
using Spectre.Console;

namespace Nox.Cli;

internal static class ConfiguratorExtensions
{
    public static IConfigurator AddNoxCommands(this IConfigurator cliConfig, IServiceCollection services, bool isOnline, string onlineCacheUrl = "")
    {
        var persistedTokenCache = services.BuildServiceProvider().GetRequiredService<IPersistedTokenCache>();
        var cacheBuilder = new NoxCliCacheBuilder(onlineCacheUrl, persistedTokenCache)
            .WithBuildEventHandler((sender, args) =>
            {
                AnsiConsole.MarkupLine(args.SpectreMessage);
            });
         
// #if DEBUG
//         cacheBuilder.WithLocalWorkflowPath("../../tests/workflows");        
// #endif        
        
        var cacheManager = cacheBuilder.Build();

        services.AddNoxCliCacheManager(cacheManager);

        if (cacheManager.Manifest?.Authentication != null)
        {
            var authValidator = new CliAuthValidator();
            authValidator.ValidateAndThrow(cacheManager.Manifest.Authentication);
            
            services.AddSingleton<ICliAuthConfiguration>(cacheManager.Manifest.Authentication);
            services.AddAzureAuthentication();
        }

        if (cacheManager.Manifest?.LocalTaskExecutor != null)
        {
            services.AddSingleton<ILocalTaskExecutorConfiguration>(cacheManager.Manifest!.LocalTaskExecutor);
        }
        
        if (cacheManager.Manifest?.RemoteTaskExecutor != null)
        {
            var rteValidator = new RemoteTaskExecutorValidator();
            rteValidator.ValidateAndThrow(cacheManager.Manifest.RemoteTaskExecutor);
            services.AddSingleton<IRemoteTaskExecutorConfiguration>(cacheManager.Manifest.RemoteTaskExecutor);
            services.AddSingleton<INoxCliServerIntegration, NoxCliServerIntegration>();
        }

        if (cacheManager.Workflows != null && cacheManager.Workflows.Any())
        {
            var workflowsByBranch = cacheManager.Workflows!
                .OrderBy(w => w.Cli.Branch, StringComparer.OrdinalIgnoreCase)
                .ThenBy(w => w.Cli.Command)
                .GroupBy(w => w.Cli.Branch.ToLower());

            var branchDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (cacheManager.Manifest?.CliCommands != null)
            {
                branchDescriptions = cacheManager.Manifest.CliCommands.ToDictionary( m => m.Name, m => m.Description, StringComparer.OrdinalIgnoreCase);
            }

            foreach (var branch in workflowsByBranch)
            {
                cliConfig.AddBranch(branch.Key, b =>
                {
                    if (branchDescriptions.ContainsKey(branch.Key))
                    {
                        b.SetDescription(branchDescriptions[branch.Key]);
                    }

                    foreach(var workflow in branch)
                    {
                        var cmdConfigContinuation = b.AddCommand<DynamicCommand>(workflow.Cli.Command)
                            .WithData(workflow)
                            .WithAlias(workflow.Cli.CommandAlias ?? string.Empty)
                            .WithDescription(workflow.Cli.Description ?? string.Empty);
                        
                        foreach (var example in workflow.Cli.Examples!)
                        {
                            cmdConfigContinuation = cmdConfigContinuation.WithExample(example.ToArray());
                        };
                    
                    }

                });
            }
        }

        return cliConfig;
    }
}

