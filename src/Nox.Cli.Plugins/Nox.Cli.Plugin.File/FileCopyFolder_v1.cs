using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.File;

public class FileCopyFolder_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "file/copy-folder@v1",
            Author = "Jan Schutte",
            Description = "Copy all files and folders inside a folder to another folder.",

            Inputs =
            {
                ["source-path"] = new NoxActionInput {
                    Id = "source-path",
                    Description = "The path to the folder from which all files will be copied",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["target-path"] = new NoxActionInput {
                    Id = "target-path",
                    Description = "The path to the folder into which to copy the source files",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["is-recursive"] = new NoxActionInput {
                    Id = "include-root",
                    Description = "Indicate whether the copy must recurse into all sub folders.",
                    Default = true,
                    IsRequired = false
                },
                ["is-overwrite"] = new NoxActionInput {
                    Id = "is-overwrite",
                    Description = "Indicate whether the copy must overwrite the target path.",
                    Default = false,
                    IsRequired = false
                } 
            }
        };
    }

    private string? _sourcePath;
    private string? _targetPath;
    private bool? _isRecursive;
    private bool? _isOverwrite;
    
    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _sourcePath = inputs.Value<string>("source-path");
        _targetPath = inputs.Value<string>("target-path");
        _isRecursive = inputs.ValueOrDefault<bool>("is-recursive", this);
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
            ctx.SetErrorMessage("The File copy-folder action was not initialized");
        }
        else
        {
            try
            {
                var fullSourcePath = Path.GetFullPath(_sourcePath);
                if (!Directory.Exists(fullSourcePath))
                {
                    ctx.SetErrorMessage($"Folder {fullSourcePath} does not exist!");
                    isValid = false;
                }
                else
                {
                    var fullTargetPath = Path.GetFullPath(_targetPath);
                    if (!Directory.Exists(fullTargetPath))
                    {
                        Directory.CreateDirectory(fullTargetPath);
                    }
                    else
                    {
                        if (_isOverwrite!.Value)
                        {
                            PurgeFolder(fullTargetPath);
                        }
                        else
                        {
                            ctx.SetErrorMessage($"Folder {fullTargetPath} already exists, and is-overwrite was not specified.");
                            isValid = false;
                        }
                    }

                    if (isValid)
                    {
                        CopyFiles(fullSourcePath, fullTargetPath);
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

    private void CopyFiles(string sourceFolder, string targetFolder)
    {
        var di = new DirectoryInfo(sourceFolder);
                        
        foreach (var file in di.GetFiles())
        {
            var targetFilePath = Path.Combine(targetFolder, file.Name); 
            if (System.IO.File.Exists(targetFilePath)) System.IO.File.Delete(targetFilePath);
            Directory.CreateDirectory(targetFolder);
            file.CopyTo(targetFilePath);
        }

        if (_isRecursive == true)
        {
            foreach (var subFolder in di.GetDirectories())
            {
                CopyFiles(subFolder.FullName, Path.Combine(targetFolder, subFolder.Name));
            }
        }
    }

    private void PurgeFolder(string path)
    {
        var di = new DirectoryInfo(path);
        foreach (var file in di.GetFiles())
        {
            file.Delete();
        }

        foreach (var dir in di.GetDirectories())
        {
            dir.Delete(true);
        }
    }
}
