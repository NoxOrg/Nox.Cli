using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.File;

public class FileRename_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "file/rename-file@v1",
            Author = "Jan Schutte",
            Description = "Rename a file.",

            Inputs =
            {
                ["source-path"] = new NoxActionInput {
                    Id = "source-path",
                    Description = "The path to the file to be renamed",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["new-name"] = new NoxActionInput {
                    Id = "new-name",
                    Description = "The new name of the file",
                    Default = string.Empty,
                    IsRequired = true
                }
            }
        };
    }

    private string? _sourcePath;
    private string? _newName;
    
    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _sourcePath = inputs.Value<string>("source-path");
        _newName = inputs.Value<string>("new-name");
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_sourcePath) ||
            string.IsNullOrEmpty(_newName))
        {
            ctx.SetErrorMessage("The File rename-file action was not initialized");
        }
        else
        {
            try
            {
                var fullSourcePath = Path.GetFullPath(_sourcePath);
                if (!System.IO.File.Exists(fullSourcePath))
                {
                    ctx.SetErrorMessage($"File: {fullSourcePath} does not exist!");
                }
                else
                {
                    var sourcePath = Path.GetDirectoryName(fullSourcePath);
                    System.IO.File.Move(fullSourcePath, Path.Combine(sourcePath!, _newName));
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