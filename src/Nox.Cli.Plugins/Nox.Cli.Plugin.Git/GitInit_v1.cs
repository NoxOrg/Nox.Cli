using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.Git;

public class GitInit_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "git/init@v1",
            Author = "Jan Schutte",
            Description = "Initialize/re-initialize a git repository in the specified working directory.",

            Inputs =
            {
                ["path"] = new NoxActionInput {
                    Id = "path",
                    Description = "The path to the working folder in which to execute the git command.",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["branch-name"] = new NoxActionInput {
                    Id = "branch-name",
                    Description = "The name of the default branch to create. Defaults to main",
                    Default = "main",
                    IsRequired = false
                }
            },

            Outputs =
            {
                ["result"] = new NoxActionOutput
                {
                    Id = "result",
                    Description = "The message returned from the git command, if any."
                },
            }
        };
    }

    private string? _path;
    private string? _branchName;
    
    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _path = inputs.Value<string>("path");
        _branchName = inputs.ValueOrDefault<string>("branch-name", this);
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_path) ||
            string.IsNullOrEmpty(_branchName))
        {
            ctx.SetErrorMessage("The Git init action was not initialized");
        }
        else
        {
            try
            { var fullPath = Path.GetFullPath(_path);
                if (!Directory.Exists(fullPath))
                {
                    ctx.SetState(ActionState.Error);
                    ctx.SetErrorMessage("Working folder does not exist!");
                }
                else
                {
                    var client = new GitClient(fullPath);
                    var response = await client.Init(_branchName);
                    if (response.Status == GitCommandStatus.Error)
                    {
                        ctx.SetState(ActionState.Error);
                        ctx.SetErrorMessage(response.Message);
                    }
                    else
                    {
                        ctx.SetState(ActionState.Success);
                    }

                    outputs["result"] = response.Message;
                }
            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage(ex.Message);
            }
        }

        return outputs;
    }

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }
}
