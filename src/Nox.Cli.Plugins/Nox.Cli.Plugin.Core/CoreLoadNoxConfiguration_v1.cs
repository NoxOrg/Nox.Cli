using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Core.Builders;

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
                var projectConfig = new ProjectConfigurationBuilder(fullPath)
                    .Build();
                if (projectConfig == null)
                {
                    ctx.SetErrorMessage($"Unable to load Nox project configuration from {fullPath}");
                }
                else
                {
                    ctx.SetProjectConfiguration(projectConfig);
                    
                    ctx.SetState(ActionState.Success);
                }
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