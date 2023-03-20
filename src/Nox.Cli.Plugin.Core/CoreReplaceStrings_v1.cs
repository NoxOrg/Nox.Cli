using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.Core;

public class CoreReplaceStrings_v1: INoxCliAddin
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
                    Description = "The source string in which values will be found and replaced",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["replacements"] = new NoxActionInput {
                    Id = "replacements",
                    Description = "a List containing strings to find and their replacement values.",
                    Default = new Dictionary<string, string>(),
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["result"] = new NoxActionOutput
                {
                    Id = "result",
                    Description = "The resulting string after the values have been replaced"
                },
            }
        };
    }
    
    private string? _source;
    private Dictionary<string, string>? _replacements;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _source = inputs.Value<string>("source-string");
        _replacements = inputs.Value<Dictionary<string, string>>("replacements");
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_source) || _replacements == null || _replacements.Count == 0)
        {
            ctx.SetErrorMessage("The Core replace-strings action was not initialized");
        }
        else
        {
            try
            {
                var result = Replace(_source, _replacements);
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
    
    private string Replace(string source, Dictionary<string, string> replacements)
    {
        var pattern = "";
        foreach (var replacement in replacements)
        {
            if (!string.IsNullOrEmpty(pattern))
            {
                pattern += "|";
            }

            pattern += replacement.Key;
        }

        var regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(2));
        var eval = new MatchEvaluator(match =>
        {
            if (replacements.ContainsKey(match.Value))
            {
                var item = replacements[match.Value];
                return item;    
            }

            return "";
        });
        return regex.Replace(source, eval);
    }
}