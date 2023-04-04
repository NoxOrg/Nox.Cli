using System.Globalization;
using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.Core;

public class CoreToPascalCase_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "core/to-pascal-case@v1",
            Author = "Jan Schutte",
            Description = "Convert a string to Pascal case.",

            Inputs =
            {
                ["source-string"] = new NoxActionInput {
                    Id = "source-string",
                    Description = "The source string which to convert to Pascal case",
                    Default = string.Empty,
                    IsRequired = true
                }
            },

            Outputs =
            {
                ["result"] = new NoxActionOutput
                {
                    Id = "result",
                    Description = "The resulting Pascal case string."
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
            ctx.SetErrorMessage("The Core to-pascal-case action was not initialized");
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
                    var txtInfo = CultureInfo.InvariantCulture.TextInfo;
                    var result = txtInfo.ToTitleCase(_source)
                        .Replace(" ", string.Empty)
                        .Replace(".", "")
                        .Replace("-", "")
                        .Replace("_", "");
                    outputs["result"] = result;
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