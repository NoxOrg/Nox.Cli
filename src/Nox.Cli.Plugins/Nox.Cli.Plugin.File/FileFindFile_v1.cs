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
    
    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _path = inputs.Value<string>("path");
        _filename = inputs.Value<string>("file-name");
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
            {
                var fullPath = Path.GetFullPath(_path);
                if (!Directory.Exists(fullPath))
                {
                    outputs["is-found"] = false;
                }
                else
                {
                    if (_filename.Contains('*'))
                    {
                        var files = Directory.GetFiles(fullPath, _filename);
                        if (files.Length == 1)
                        {
                            outputs["is-found"] = true;
                        }
                        else
                        {
                            outputs["is-found"] = false;
                        }
                    }
                    else
                    {
                        var fullFilename = Path.Combine(fullPath, _filename);
                        if (System.IO.File.Exists(fullFilename))
                        {
                            outputs["is-found"] = true;
                        }
                        else
                        {
                            outputs["is-found"] = false;
                        }    
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
