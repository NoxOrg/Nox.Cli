using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.Core;

public class CoreGetStringCapitals_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "core/get-string-capitals@v1",
            Author = "Jan Schutte",
            Description = "Get all the capital letters in a string.",

            Inputs =
            {
                ["source-string"] = new NoxActionInput {
                    Id = "source-string",
                    Description = "The source string from which to get the capital letters",
                    Default = string.Empty,
                    IsRequired = true
                }
            },

            Outputs =
            {
                ["result"] = new NoxActionOutput
                {
                    Id = "result",
                    Description = "The resulting concatenated capital letters."
                },
                ["lower-result"] = new NoxActionOutput
                {
                    Id = "lower-result",
                    Description = "The resulting concatenated capital letters converted to lower case."
                }
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
            ctx.SetErrorMessage("The Core get-string-capitals action was not initialized");
        }
        else
        {
            try
            {
                if(_source.Length < 2) {
                    outputs["result"] = _source;
                    outputs["lower-result"] = _source.ToLower();
                }
                else
                {
                    var capitals = string.Concat(_source.Where(c => c >= 'A' && c <= 'Z'));
                    outputs["result"] = capitals;
                    outputs["lower-result"] = capitals.ToLower();
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