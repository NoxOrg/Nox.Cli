using System.Text.RegularExpressions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.Actions;

namespace Nox.Cli.Plugin.File;

public class FileReplaceStrings_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "file/replace-strings@v1",
            Author = "Jan Schutte",
            Description = "Replace one or more strings in a file.",

            Inputs =
            {
                ["path"] = new NoxActionInput {
                    Id = "path",
                    Description = "The path to the file containing the strings to replace",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["replacements"] = new NoxActionInput {
                    Id = "replacements",
                    Description = "List containing a strings to replace and their replacement values.",
                    Default = new Dictionary<string, string>(),
                    IsRequired = true
                },
            }
        };
    }

    private string? _path;
    private Dictionary<string, string>? _replacements;

    public Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string,object> inputs)
    {
        _path = inputs.Value<string>("path");
        _replacements = inputs.Value<Dictionary<string, string>>("replacements");
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_path) || _replacements == null || _replacements.Count == 0)
        {
            ctx.SetErrorMessage("The File purge-folder action was not initialized");
        }
        else
        {
            try
            {
                var fullPath = Path.GetFullPath(_path);
                if (!System.IO.File.Exists(fullPath))
                {
                    ctx.SetErrorMessage($"File {fullPath} does not exist.");                    
                }
                else
                {
                    var source = System.IO.File.ReadAllText(fullPath);
                    var result = Replace(source, _replacements);
                    System.IO.File.WriteAllText(fullPath, result);
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

    public Task EndAsync(INoxWorkflowContext ctx)
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

        var regex = new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
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

