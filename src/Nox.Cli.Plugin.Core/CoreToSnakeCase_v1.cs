using System.Text;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.Core;

public class CoreToSnakeCase_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "core/replace-strings@v1",
            Author = "Jan Schutte",
            Description = "Replace one or more strings in a source string.",

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
                    var sb = new StringBuilder();
                    sb.Append(char.ToLowerInvariant(_source[0]));
                    for(int i = 1; i < _source.Length; ++i) {
                        char c = _source[i];
                        if (c != '.')
                        {
                            if(char.IsUpper(c)) {
                                sb.Append('_');
                                sb.Append(char.ToLowerInvariant(c));
                            } else {
                                sb.Append(c);
                            }    
                        }
                    }
                    outputs["result"] = sb.ToString();    
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