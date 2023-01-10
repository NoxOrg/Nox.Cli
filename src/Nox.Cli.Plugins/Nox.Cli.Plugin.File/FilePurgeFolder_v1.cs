using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.Actions;

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
                }
            }
        };
    }

    private string? _path;
    
    public Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string,object> inputs)
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
            ctx.SetErrorMessage("The File purge-folder action was not initialized");
        }
        else
        {
            try
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

                ctx.SetState(ActionState.Success);
            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage(ex.Message);
            }
        }
        
        return Task.FromResult<IDictionary<string, object>>(outputs);
    }

    public Task EndAsync(INoxWorkflowContext ctx)
    {
        return Task.CompletedTask;
    }
}

