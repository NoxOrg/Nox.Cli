using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.File;

public class FileSetCurrentDirectory_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "file/set-current-directory@v1",
            Author = "Jan Schutte",
            Description = "Set the current directory.",

            Inputs =
            {
                ["path"] = new NoxActionInput {
                    Id = "path",
                    Description = "The path to the directory to set as current.",
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
            ctx.SetErrorMessage("The File set-current-directory action was not initialized");
        }
        else
        {
            try
            {
                if (ctx.IsServer) throw new NoxCliException("Cannot set the current directory on the server!");
                var fullPath = Path.GetFullPath(_path);
                if (!Directory.Exists(fullPath)) throw new NoxCliException("Path to set does not exist!");

                Directory.SetCurrentDirectory(fullPath);
                
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