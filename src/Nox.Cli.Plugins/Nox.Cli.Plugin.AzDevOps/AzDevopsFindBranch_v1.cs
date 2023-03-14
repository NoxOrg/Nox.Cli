using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsFindBranch_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/find-branch@v1",
            Author = "Jan Schutte",
            Description = "Find a branch in an Azure Devops repository",

            Inputs =
            {
                ["connection"] = new NoxActionInput {
                    Id = "connection",
                    Description = "The connection established with action 'azdevops/connect@v1'",
                    Default = new VssConnection(new Uri("https://localhost"), null),
                    IsRequired = true
                },
                ["repository-id"] = new NoxActionInput {
                    Id = "repository-id",
                    Description = "The Id (Guid) of the devops repository. Normally the output from 'azdevops/get-repo@v1'",
                    Default = Guid.Empty,
                    IsRequired = true
                },
                ["branch-name"] = new NoxActionInput { 
                    Id = "branch-name", 
                    Description = "The name of the branch to find",
                    Default = string.Empty,
                    IsRequired = true
                }
            },

            Outputs =
            {
                ["is-found"] = new NoxActionOutput {
                    Id = "is-found",
                    Description = "returns true if the branch exists, else returns false",
                },
            }
        };
    }

    private GitHttpClient? _gitClient;
    private Guid? _repoId;
    private string? _branchName;
    private bool _isServerContext;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _gitClient = await connection!.GetClientAsync<GitHttpClient>();
        _repoId = inputs.Value<Guid>("repository-id");
        _branchName = inputs.Value<string>("branch-name");
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_gitClient == null || _repoId == null || _repoId == Guid.Empty || string.IsNullOrEmpty(_branchName))
        {
            ctx.SetErrorMessage("The devops find-branch action was not initialized");
        }
        else
        {
            try
            {
                var repoId = Guid.NewGuid();
                var branches = await _gitClient.GetBranchesAsync(_repoId.Value.ToString());
                outputs["is-found"] = branches.Any(b => String.Equals(b.Name, _branchName, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                outputs["is-found"] = false;

            }
            ctx.SetState(ActionState.Success);
        }

        return outputs;
    }

    public Task EndAsync()
    {
        if (!_isServerContext) _gitClient?.Dispose();
        return Task.CompletedTask;
    }
}