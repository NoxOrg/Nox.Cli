using Nox.Cli.Actions;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsPurgeRepoCache_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/purge-repo-cache@v1",
            Author = "Jan Schutte",
            Description = "Purge the Nox local repository cache.",

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

