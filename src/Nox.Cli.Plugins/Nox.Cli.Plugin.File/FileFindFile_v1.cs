using System.Text;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.File;

public class FileFindFile_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "file/find-file@v1",
            Author = "Jan Schutte",
            Description = "Find a file inside a folder.",

            Inputs =
            {
                ["path"] = new NoxActionInput {
                    Id = "path",
                    Description = "The path to the folder to search",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["file-name"] = new NoxActionInput {
                    Id = "file-name",
                    Description = "The name of the file to find, must include the file extension.",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["include-sub-folders"] = new NoxActionInput {
                    Id = "include-sub-folders",
                    Description = "Set to include any sub folders in the path.",
                    Default = false,
                    IsRequired = false
                }
            },

            Outputs =
            {
                ["is-found"] = new NoxActionOutput
                {
                    Id = "is-found",
                    Description = "Indicates if the file exists or not."
                },
            }
        };
    }

    private string? _path;
    private string? _filename;
    private bool? _includeSubFolders;
    
    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _path = inputs.Value<string>("path");
        _filename = inputs.Value<string>("file-name");
        _includeSubFolders = inputs.ValueOrDefault<bool>("include-sub-folders", this);
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_path) ||
            string.IsNullOrEmpty(_filename))
        {
            ctx.SetErrorMessage("The File find-file action was not initialized");
        }
        else
        {
            try
            { var fullPath = Path.GetFullPath(_path);
                if (!Directory.Exists(fullPath))
                {
                    outputs["is-found"] = false;
                }
                else
                {
                    string[] files;
                    if (_includeSubFolders == true)
                    {
                        files = Directory.GetFiles(fullPath, _filename, SearchOption.AllDirectories);
                    }
                    else
                    {
                        files = Directory.GetFiles(fullPath, _filename, SearchOption.TopDirectoryOnly);
                    }
                    if (files.Length > 0)
                    {
                        outputs["is-found"] = true;
                    }
                    else
                    {
                        outputs["is-found"] = false;
                    }
                }
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
}
