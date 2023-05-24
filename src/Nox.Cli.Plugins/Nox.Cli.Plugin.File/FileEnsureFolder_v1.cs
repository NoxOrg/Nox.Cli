using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.File;

public class FileEnsureFolder_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "file/ensure-folder@v1",
            Author = "Jan Schutte",
            Description = "Create a sub folder for a path if it does not exist.",

            Inputs =
            {
                ["path"] = new NoxActionInput {
                    Id = "path",
                    Description = "The path where the new folder must be created",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["folder-name"] = new NoxActionInput {
                    Id = "directory-name",
                    Description = "The name of the folder to create.",
                    Default = string.Empty,
                    IsRequired = false
                },
            }
        };
    }

    private string? _path;
    private string? _folderName;
    
    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _path = inputs.Value<string>("path");
        _folderName = inputs.Value<string>("folder-name");
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_path) ||
            string.IsNullOrEmpty(_folderName))
        {
            ctx.SetErrorMessage("The File create-folder action was not initialized");
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
                    fullPath = Path.Combine(fullPath, _folderName);
                    Directory.CreateDirectory(fullPath);
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