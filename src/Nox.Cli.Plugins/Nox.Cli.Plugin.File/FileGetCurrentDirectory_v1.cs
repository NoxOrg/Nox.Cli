using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Exceptions;

namespace Nox.Cli.Plugin.File;

public class FileGetCurrentDirectory_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "file/get-current-directory@v1",
            Author = "Jan Schutte",
            Description = "Get the current directory.",

            Outputs =
            {
                ["path"] = new NoxActionOutput
                {
                    Id = "path",
                    Description = "Contains the full path of the current directory."
                },
                
                ["directory-name"] = new NoxActionOutput
                {
                    Id = "directory-name",
                    Description = "Contains the current directory name."
                },
            }
        };
    }
    
    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        try
        {
            if (ctx.IsServer) throw new NoxCliException("Cannot get the current directory on the server!");
            var fullPath = Directory.GetCurrentDirectory();
            outputs["path"] = fullPath;
            outputs["directory-name"] = Path.GetFileName(fullPath);

            ctx.SetState(ActionState.Success);
        }
        catch (Exception ex)
        {
            ctx.SetErrorMessage(ex.Message);
        }
        
        return Task.FromResult<IDictionary<string, object>>(outputs);
    }

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }
}