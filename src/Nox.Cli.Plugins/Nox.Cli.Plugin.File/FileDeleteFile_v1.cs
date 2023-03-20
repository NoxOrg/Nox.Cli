using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.File;

public class FileDeleteFile_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "file/delete-file@v1",
            Author = "Jan Schutte",
            Description = "Delete a file in a folder.",

            Inputs =
            {
                ["path"] = new NoxActionInput {
                    Id = "path",
                    Description = "The path to the file to delete",
                    Default = string.Empty,
                    IsRequired = true
                }
            }
        };
    }

    private string? _path;
    
    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _path = inputs.Value<string>("path");
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_path))
        {
            ctx.SetErrorMessage("The File delete-file action was not initialized");
        }
        else
        {
            try
            {
                var fullPath = Path.GetFullPath(_path);
                if (!Directory.Exists(fullPath))
                {
                    ctx.SetErrorMessage($"Folder {fullPath} does not exist!");
                }
                else
                {
                    System.IO.File.Delete(fullPath);
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

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }
}