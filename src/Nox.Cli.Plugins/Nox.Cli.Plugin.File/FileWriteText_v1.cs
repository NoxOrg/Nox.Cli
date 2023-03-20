using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.File;

public class FileWriteText_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "file/write-text@v1",
            Author = "Jan Schutte",
            Description = "Write text to a file. Replaces the current contents if the file exists.",

            Inputs =
            {
                ["path"] = new NoxActionInput {
                    Id = "path",
                    Description = "The path to the file to write.",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["text-to-write"] = new NoxActionInput
                {
                    Id = "text-to-write",
                    Description = "The string to to write to the file.",
                    Default = string.Empty,
                    IsRequired = true
                }
            }
        };
    }
    
    private string? _path;
    private string? _textToWrite;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _path = inputs.Value<string>("path");
        _textToWrite = inputs.Value<string>("text-to-write");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_path) ||
            string.IsNullOrEmpty(_textToWrite))
        {
            ctx.SetErrorMessage("The File write-text action was not initialized");
        }
        else
        {
            try
            {
                var fullPath = Path.GetFullPath(_path);
                await System.IO.File.WriteAllTextAsync(fullPath, _textToWrite);
                ctx.SetState(ActionState.Success);    
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