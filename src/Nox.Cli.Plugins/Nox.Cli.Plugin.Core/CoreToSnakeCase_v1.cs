using System.Text;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.Core;

public class CoreToSnakeCase_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "core/to-snake-case@v1",
            Author = "Jan Schutte",
            Description = "Convert a string to snake case.",

            Inputs =
            {
                ["source-string"] = new NoxActionInput {
                    Id = "source-string",
                    Description = "The source string which to convert to snake case",
                    Default = string.Empty,
                    IsRequired = true
                }
            },

            Outputs =
            {
                ["result"] = new NoxActionOutput
                {
                    Id = "result",
                    Description = "The resulting snake case string."
                },
                ["result-lower"] = new NoxActionOutput
                {
                    Id = "result-lower",
                    Description = "The resulting snake case string, converted to lower case."
                },
                ["result-upper"] = new NoxActionOutput
                {
                    Id = "result-upper",
                    Description = "The resulting snake case string, converted to upper case."
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
            ctx.SetErrorMessage("The Core to-snake-case action was not initialized");
        }
        else
        {
            try
            {
                if(_source.Length < 2) {
                    outputs["result"] = _source;
                }
                else
                {
                    var result = _source.Replace('.', '_');
                    outputs["result"] = result;
                    outputs["result-lower"] = result.ToLower();
                    outputs["result-upper"] = result.ToUpper();
                }
                
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