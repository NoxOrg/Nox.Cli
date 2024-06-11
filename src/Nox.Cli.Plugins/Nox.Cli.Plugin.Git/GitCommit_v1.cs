using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.Git;

public class GitCommit_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "git/commit@v1",
            Author = "Jan Schutte",
            Description = "Record changes to the repository.",

            Inputs =
            {
                ["path"] = new NoxActionInput {
                    Id = "path",
                    Description = "The path to the working folder in which to execute the git command.",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["commit-message"] = new NoxActionInput {
                    Id = "commit-message",
                    Description = "The message to associate with the commit.",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["suppress-warnings"] = new NoxActionInput {
                    Id = "suppress-warnings",
                    Description = "Indicate whether the plugin should ignore warnings.",
                    Default = false,
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
    private string? _message;
    private bool? _suppressWarnings;
    
    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _path = inputs.Value<string>("path");
        _message = inputs.Value<string>("commit-message");
        _suppressWarnings = inputs.ValueOrDefault<bool>("suppress-warnings", this);
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_path) ||
            string.IsNullOrEmpty(_message) ||
            _suppressWarnings == null)
        {
            ctx.SetErrorMessage("The Git commit action was not initialized");
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
                    var response = await client.Commit(_message, _suppressWarnings.Value);
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
