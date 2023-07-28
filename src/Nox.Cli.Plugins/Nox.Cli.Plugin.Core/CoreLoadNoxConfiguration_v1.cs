using System.Reflection;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Solution;

namespace Nox.Cli.Plugin.Core;

public class CoreLoadNoxConfiguration_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "core/load-nox-configuration@v1",
            Author = "Jan Schutte",
            Description = "Load a Nox project configuration (yaml) from a path.",

            Inputs =
            {
                ["path"] = new NoxActionInput {
                    Id = "path",
                    Description = "The relative path to the Nox project configuration to load.",
                    Default = string.Empty,
                    IsRequired = true
                }
            }
        };
    }
    
    private string? _path;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _path = inputs.Value<string>("path");
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        if (ctx.IsServer) throw new NoxCliException("This action cannot be executed on a server. remove the run-at-server attribute for this step in your Nox workflow.");
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_path))
        {
            ctx.SetErrorMessage("The Core load-nox-configuration action was not initialized");
        }
        else
        {
            try
            {
                var fullPath = Path.GetFullPath(_path);
                var solution = new NoxSolutionBuilder()
                    .OnResolveSecrets((_, args) =>
                    {
                        var secretsConfig = args.SecretsConfig;
                        var secretKeys =  args.Variables;
                        var resolver = ctx.NoxSecretsResolver;
                        if (resolver == null) throw new NoxCliException("Cannot load Nox solution definition, Secrets resolved has not been initialized.");
                        resolver.Configure(secretsConfig!, Assembly.GetEntryAssembly());
                        args.Secrets = resolver.Resolve(secretKeys!);
                    })
                    .Build();
                ctx.SetProjectConfiguration(solution);
                    
                ctx.SetState(ActionState.Success);
            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage(ex.Message);
            }
        }
        
        return Task.FromResult<IDictionary<string, object>>(outputs);
    }

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }
}