using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.File;

public class FileReadText_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "file/read-text@v1",
            Author = "Jan Schutte",
            Description = "Read the text contents of a file.",

            Inputs =
            {
                ["path"] = new NoxActionInput {
                    Id = "path",
                    Description = "The path to the file to read.",
                    Default = string.Empty,
                    IsRequired = true
                }
            },

            Outputs =
            {
                ["result-string"] = new NoxActionOutput
                {
                    Id = "result-string",
                    Description = "The resulting string contents of the read file"
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
            ctx.SetErrorMessage("The File read-text action was not initialized");
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
                    var result = await System.IO.File.ReadAllTextAsync(fullPath);
                    outputs["result-string"] = result;
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