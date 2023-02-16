using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsEnsureRepoExists_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/ensure-repo-exists@v1",
            Author = "Jan Schutte",
            Description = "Get a reference to a DevOps repository, if it does not exist then create it.",

            Inputs =
            {
                ["connection"] = new NoxActionInput {
                    Id = "connection",
                    Description = "The connection established with action 'azdevops/connect@v1'",
                    Default = new VssConnection(new Uri("https://localhost"), null),
                    IsRequired = true
                },
                ["project-id"] = new NoxActionInput { 
                    Id = "project-id", 
                    Description = "The DevOps project Identifier",
                    Default = Guid.Empty,
                    IsRequired = true
                },
                ["repository-name"] = new NoxActionInput { 
                    Id = "repository-name", 
                    Description = "The DevOps repository name",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["default-branch"] = new NoxActionInput { 
                    Id = "default-branch", 
                    Description = "The default branch for this DevOps repository",
                    Default = "main",
                    IsRequired = false
                },
            },

            Outputs =
            {
                ["repository-id"] = new NoxActionOutput {
                    Id = "repository-id",
                    Description = "The Azure devops repository id",
                },
                ["success-message"] = new NoxActionOutput {
                    Id = "success-message",
                    Description = "A message specifying if the repo was found or created",
                },
            }
        };
    }

    private GitHttpClient? _repoClient;
    private string? _repoName;
    private Guid? _projectId;
    private string? _defaultBranch;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectId = inputs.Value<Guid>("project-id");
        _repoName = inputs.Value<string>("repository-name");
        _defaultBranch = inputs.ValueOrDefault<string>("default-branch", this);
        _repoClient = await connection!.GetClientAsync<GitHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_repoClient == null || string.IsNullOrEmpty(_repoName) || _projectId == null || _projectId == Guid.Empty)
        {
            ctx.SetErrorMessage("The devops create-repo action was not initialized");
        }
        else
        {
            try
            {
                var repo = await _repoClient.GetRepositoryAsync(_projectId.Value, _repoName);
                if(repo != null)
                {
                    outputs["repository-id"] = repo.Id;
                    outputs["success-message"] = $"Found existing repo {_repoName} ({repo.Id})";
                    ctx.SetState(ActionState.Success);
                }
            }
            catch
            {
                try
                {
                    //Create the Repo
                    var repo = await CreateRepositoryAsync(ctx);
                    if (repo != null)
                    {
                        outputs["repository-id"] = repo.Id;
                        outputs["success-message"] = $"Created repo {_repoName} ({repo.Id})";
                        ctx.SetState(ActionState.Success);
                    }
                }
                catch(Exception ex)
                {
                    ctx.SetErrorMessage(ex.Message);
                }
            }
        }

        return outputs;
    }

    public Task EndAsync()
    {
        _repoClient?.Dispose();
        return Task.CompletedTask;
    }
    
    private async Task<GitRepository?> CreateRepositoryAsync(INoxWorkflowContext ctx)
    {
        var repo = new GitRepository
        {
            DefaultBranch = _defaultBranch!,
            Name = _repoName!,
            ProjectReference = new TeamProjectReference
            {
                Id = _projectId!.Value
            }
        };

        try
        {
            repo = await _repoClient!.CreateRepositoryAsync(repo);
            return repo;
        }
        catch (Exception ex)
        {
            ctx.SetErrorMessage(ex.Message);
        }

        return null;
    }
}

