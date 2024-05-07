using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.File;

public class FileCopyFile_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "file/copy-file@v1",
            Author = "Jan Schutte",
            Description = "Copy a file from one folder to another.",

            Inputs =
            {
                ["source-path"] = new NoxActionInput {
                    Id = "source-path",
                    Description = "The path to the file to copy",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["target-path"] = new NoxActionInput {
                    Id = "target-path",
                    Description = "The path of the folder into which to copy the source file",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["is-overwrite"] = new NoxActionInput {
                    Id = "is-overwrite",
                    Description = "Indicate whether the copy must overwrite the target file, if it exists.",
                    Default = false,
                    IsRequired = false
                } 
            }
        };
    }

    private string? _sourcePath;
    private string? _targetPath;
    private bool? _isOverwrite;
    
    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _sourcePath = inputs.Value<string>("source-path");
        _targetPath = inputs.Value<string>("target-path");
        _isOverwrite = inputs.ValueOrDefault<bool>("is-overwrite", this);
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();
        var isValid = true;

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_sourcePath) ||
            string.IsNullOrEmpty(_targetPath))
        {
            ctx.SetErrorMessage("The File copy-file action was not initialized");
        }
        else
        {
            try
            {
                var fullSourcePath = Path.GetFullPath(_sourcePath);
                var filename = Path.GetFileName(fullSourcePath);
                if (!System.IO.File.Exists(fullSourcePath))
                {
                    ctx.SetErrorMessage($"Source file: {fullSourcePath} does not exist!");
                    isValid = false;
                }
                else
                {
                    var fullTargetPath = Path.Combine(Path.GetFullPath(_targetPath), filename);
                    Directory.CreateDirectory(Path.GetDirectoryName(fullTargetPath)!);
                    if (System.IO.File.Exists(fullTargetPath))
                    {
                        if (_isOverwrite!.Value)
                        {
                            System.IO.File.Delete(fullTargetPath);    
                        }
                        else
                        {
                            ctx.SetErrorMessage($"File: {fullTargetPath} already exists, and is-overwrite was not specified.");
                            isValid = false;
                        }
                        
                    }
                    if (isValid)
                    {
                        System.IO.File.Copy(fullSourcePath, fullTargetPath);
                        ctx.SetState(ActionState.Success);  
                    }
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
