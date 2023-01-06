using Nox.Cli.Actions;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsCloneRepo_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/clone-repo@v1",
            Author = "Jan Schutte",
            Description = "Get an Azure Devops repository",

            Inputs =
            {
                ["connection"] = new NoxActionInput {
                    Id = "connection",
                    Description = "The connection established with action 'azdevops/connect@v1'",
                    Default = new VssConnection(new Uri("https://localhost"), null),
                    IsRequired = true
                },
                ["repository"] = new NoxActionInput {
                    Id = "repository",
                    Description = "a reference to a devops repository. Normally the output from 'azdevops/get-repo@v1'",
                    Default = new GitRepository(),
                    IsRequired = true
                },
                ["branch-name"] = new NoxActionInput { 
                    Id = "branch-name", 
                    Description = "The name of the branch to clone, defaults to 'main'",
                    Default = string.Empty,
                    IsRequired = false
                }
            },

            Outputs =
            {
                ["repository-path"] = new NoxActionOutput {
                    Id = "repository-path",
                    Description = "The local path where the repository was cloned",
                },
            }
        };
    }

    private GitHttpClient? _repoClient;
    private GitRepository? _repo;
    private string? _branchName;

    public async Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string,object> inputs)
    {
        var connection = (VssConnection)inputs["connection"];
        _repoClient = await connection.GetClientAsync<GitHttpClient>();
        _repo = (GitRepository)inputs["repository"]; 
        _branchName = (string)inputs["branch-name"];
        if (string.IsNullOrEmpty(_branchName)) _branchName = (string)this.DefaultValue("branch-name");
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_repoClient == null || _repo == null || string.IsNullOrEmpty(_branchName))
        {
            ctx.SetErrorMessage("The devops clone-repo action was not initialized");
        }
        else
        {
            try
            {
                var items = await _repoClient.GetItemsAsync(_repo.ProjectReference.Name, _repo.Id);
                //outputs["repository-path"] = repoPath;
            }
            catch
            {
                outputs["repository-path"] = null!;
            }
            ctx.SetState(ActionState.Success);
        }

        return outputs;
    }

    public Task EndAsync(INoxWorkflowContext ctx)
    {
        return Task.CompletedTask;
    }
}

