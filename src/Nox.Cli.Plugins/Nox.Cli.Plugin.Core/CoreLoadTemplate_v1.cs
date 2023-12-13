using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.Configuration;

namespace Nox.Cli.Plugin.Core;

public class CoreLoadTemplate_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "core/load-template@v1",
            Author = "Jan Schutte",
            Description = "Read the text contents of a nox template from the nox template cache.",

            Inputs =
            {
                ["path"] = new NoxActionInput {
                    Id = "path",
                    Description = "The relative path to the template to load.",
                    Default = string.Empty,
                    IsRequired = true
                }
            },

            Outputs =
            {
                ["result"] = new NoxActionOutput
                {
                    Id = "result",
                    Description = "The resulting string contents of the read template"
                },
            }
        };
    }
    
    private string? _path;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _path = inputs.Value<string>("path");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_path))
        {
            ctx.SetErrorMessage("The Core load-template action was not initialized");
        }
        else
        {
            try
            {
                ctx.CacheManager!.RefreshTemplate(_path);
                //Check if the template has a new online version
                
                var fullPath = Path.GetFullPath(Path.Combine(WellKnownPaths.TemplatesCachePath, _path));
                if (!System.IO.File.Exists(fullPath))
                {
                    ctx.SetErrorMessage($"Template file {fullPath} does not exist.");                    
                }
                else
                {
                    var result = await System.IO.File.ReadAllTextAsync(fullPath);
                    outputs["result"] = result;
                    ctx.SetState(ActionState.Success);    
                }
            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage(ex.Message);
            }
        }
        
        return outputs;
    }

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }
}