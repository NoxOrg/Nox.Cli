using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.File;

public class FilePurgeFolder_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "file/purge-folder@v1",
            Author = "Jan Schutte",
            Description = "Delete all files and folders inside a folder.",

            Inputs =
            {
                ["path"] = new NoxActionInput {
                    Id = "path",
                    Description = "The path to the folder to purge",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["include-root"] = new NoxActionInput {
                    Id = "include-root",
                    Description = "Indicate whether the root of the path must also be deleted.",
                    Default = false,
                    IsRequired = false
                },
            }
        };
    }

    private string? _path;
    private bool? _includeRoot;
    
    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _path = inputs.Value<string>("path");
        _includeRoot = inputs.ValueOrDefault<bool>("include-root", this);
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_path))
        {
            ctx.SetErrorMessage("The File purge-folder action was not initialized");
        }
        else
        {
            try
            {
                var fullPath = Path.GetFullPath(_path);
                if (!Directory.Exists(fullPath))
                {
                    ctx.SetErrorMessage($"Directory {fullPath} does not exist!");
                }
                else
                {
                    var di = new DirectoryInfo(_path);

                    foreach (var file in di.GetFiles())
                    {
                        file.Delete();
                    }

                    foreach (var dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }

                    if (_includeRoot!.Value)
                    {
                        Directory.Delete(fullPath);
                    }

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

