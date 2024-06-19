using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.Core;

public class CoreToUpperCase_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "core/to-upper-case@v1",
            Author = "Jan Schutte",
            Description = "Convert a string to upper case.",

            Inputs =
            {
                ["source-string"] = new NoxActionInput {
                    Id = "source-string",
                    Description = "The source string which to convert to upper case",
                    Default = string.Empty,
                    IsRequired = true
                }
            },

            Outputs =
            {
                ["result"] = new NoxActionOutput
                {
                    Id = "result",
                    Description = "The resulting upper case string."
                },
            }
        };
    }
    
    private string? _source;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _source = inputs.Value<string>("source-string");
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_source))
        {
            ctx.SetErrorMessage("The Core to-upper-case action was not initialized");
        }
        else
        {
            try
            {
                outputs["result"] = _source.ToUpper();
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