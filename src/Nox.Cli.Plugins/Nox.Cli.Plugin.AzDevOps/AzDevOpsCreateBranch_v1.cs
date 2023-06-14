using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.AzDevOps;

public class AzDevOpsCreateBranch_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/create-branch@v1",
            Author = "Jan Schutte",
            Description = "Create a new branch in a repository based on an existing branch",

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
                    Description = "The name of the new branch to create, if not supplied will default to Nox_Cli_<GUID>, resolved name available in outputs ",
                    Default = $"Nox_Cli_{Guid.NewGuid()}",
                    IsRequired = true
                },
                ["from-branch"] = new NoxActionInput { 
                    Id = "from-branch", 
                    Description = "The name of the branch to base the new branch on, defaults to 'main'",
                    Default = "main",
                    IsRequired = false
                }
            },

            Outputs =
            {
                ["branch-name"] = new NoxActionOutput {
                    Id = "branch-name",
                    Description = "The name of the new branch that was created",
                },
            }
        };
    }

    private GitHttpClient? _gitClient;
    private Guid? _repoId;
    private string? _branchName;
    private string? _fromBranch;
    private bool _isServerContext;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _gitClient = await connection!.GetClientAsync<GitHttpClient>();
        _repoId = inputs.Value<Guid>("repository-id");
        _branchName = inputs.ValueOrDefault<string>("branch-name", this);
        _fromBranch = inputs.ValueOrDefault<string>("from-branch", this);
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_gitClient == null || 
            _repoId == null || 
            _repoId == Guid.Empty || 
            string.IsNullOrEmpty(_branchName) ||
            string.IsNullOrEmpty(_fromBranch))
        {
            ctx.SetErrorMessage("The devops create-branch action was not initialized");
        }
        else
        {
            try
            {
                
                //Get the source branch object id
                var sourceBranches = await _gitClient.GetRefsAsync(_repoId!.Value.ToString(), filter: $"heads/{_fromBranch}");
                if (sourceBranches.Count != 1)
                {
                    ctx.SetErrorMessage("From branch does not exist in repository!");
                }
                else
                {
                    var refUpdate = new GitRefUpdate
                    {
                        OldObjectId = "0000000000000000000000000000000000000000",
                        NewObjectId = sourceBranches[0].ObjectId,
                        Name = $"refs/heads/{_branchName}"
                    };
                    await _gitClient.UpdateRefsAsync(new GitRefUpdate[] { refUpdate }, _repoId.Value.ToString());
                }

                outputs["branch-name"] = _branchName;
                ctx.SetState(ActionState.Success);
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
        if (!_isServerContext && _gitClient != null) _gitClient.Dispose();
        return Task.CompletedTask;
    }
}
