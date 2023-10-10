using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.Core;

public class CoreAppendString_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "core/append-string@v1",
            Author = "Jan Schutte",
            Description = "Append a string to a source string.",

            Inputs =
            {
                ["source-string"] = new NoxActionInput
                {
                    Id = "source-string",
                    Description = "The source string to which the string-to-append will be added.",
                    Default = string.Empty,
                    IsRequired = true
                },

                ["string-to-append"] = new NoxActionInput
                {
                    Id = "string-to-append",
                    Description = "The string to append to the source string.",
                    Default = string.Empty,
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["result"] = new NoxActionOutput
                {
                    Id = "result",
                    Description = "The resulting string after the string-to-append has been added."
                },
            }
        };
    }

    private string? _source;
    private string? _stringToAppend;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _source = inputs.Value<string>("source-string");
        _stringToAppend = inputs.Value<string>("string-to-append");
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrWhiteSpace(_stringToAppend))
        {
            ctx.SetErrorMessage("The Core append-string action was not initialized");
        }
        else
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_source)) _source = "";
                if (!_source.EndsWith(Environment.NewLine)) _source += Environment.NewLine;
                var result = _source + _stringToAppend;
                outputs["result"] = result;
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