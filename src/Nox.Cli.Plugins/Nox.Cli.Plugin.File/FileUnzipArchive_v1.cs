using Nox.Cli.Actions;

namespace Nox.Cli.Plugin.File;

public class FileUnzipArchive_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "file/purge-folder@v1",
            Author = "Jan Schutte",
            Description = "Delete all files and folders inside a folder.",

        };
    }

    public Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string,object> inputs)
    {
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        try
        {
            var repoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "nox", "repositories");
            var di = new DirectoryInfo(repoPath);

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
        return Task.FromResult<IDictionary<string, object>>(outputs);
    }

    public Task EndAsync(INoxWorkflowContext ctx)
    {
        return Task.CompletedTask;
    }
}