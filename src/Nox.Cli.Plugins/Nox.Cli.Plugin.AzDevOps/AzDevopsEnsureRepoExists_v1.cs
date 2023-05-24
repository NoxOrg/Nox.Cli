using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.AzDevOps;

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
                ["do-initialize"] = new NoxActionInput
                {
                    Id = "do-initialize",
                    Description = "When this flag is set to true, the repo will be initialized with a README.md placeholder file.",
                    Default = false,
                    IsRequired = false
                }
            },

            Outputs =
            {
                ["repository-id"] = new NoxActionOutput {
                    Id = "repository-id",
                    Description = "The Azure devops repository id",
                },
                ["is-found"] = new NoxActionOutput {
                    Id = "is-found",
                    Description = "A boolean indicating if the repo was found or created, true->found, false->created ",
                },
                ["success-message"] = new NoxActionOutput {
                    Id = "success-message",
                    Description = "A message specifying if the repo was found or created",
                },
            }
        };
    }

    private GitHttpClient? _gitClient;
    private string? _repoName;
    private Guid? _projectId;
    private string? _defaultBranch;
    private bool? _doInit;
    private bool _isServerContext = false;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectId = inputs.Value<Guid>("project-id");
        _repoName = inputs.Value<string>("repository-name");
        _defaultBranch = inputs.ValueOrDefault<string>("default-branch", this);
        _doInit = inputs.ValueOrDefault<bool>("do-initialize", this);
        _gitClient = await connection!.GetClientAsync<GitHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_gitClient == null || string.IsNullOrEmpty(_repoName) || _projectId == null || _projectId == Guid.Empty)
        {
            ctx.SetErrorMessage("The devops ensure-repo-exists action was not initialized");
        }
        else
        {
            try
            {
                var repo = await _gitClient.GetRepositoryAsync(_projectId.Value, _repoName);
                if(repo != null)
                {
                    outputs["repository-id"] = repo.Id;
                    outputs["is-found"] = true;
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
                        outputs["is-found"] = false;
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
        if (!_isServerContext) _gitClient?.Dispose();
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
            repo = await _gitClient!.CreateRepositoryAsync(repo);
            if (_doInit == true)
            {
                var commit = CreateCommit();
                var pushId = await CreatePush(repo.Id, _defaultBranch!, commit);    
            }
            return repo;
        }
        catch (Exception ex)
        {
            ctx.SetErrorMessage(ex.Message);
        }

        return null;
    }
    
    private GitCommitRef CreateCommit()
    {
        var changes = GetInitFiles();
        var result = new GitCommitRef
        {
            Comment = $"Nox Cli commit {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            Changes = changes
        };
        return result;
    }

    private async Task<int> CreatePush(Guid repoId, string branchName, GitCommitRef commit)
    {
        //var serverBranches = await _gitClient!.GetRefsAsync(_repoId!.Value.ToString(), filter: $"heads/{branchName}");
        //if (serverBranches.Count != 1) throw new NoxException($"Unable to locate branch {branchName}");
        
        var branch = new GitRefUpdate
        {
            Name = $"refs/heads/{branchName}",
            RepositoryId = repoId,
            OldObjectId = "0000000000000000000000000000000000000000",
        };
                    
        var push = await _gitClient!.CreatePushAsync(new GitPush
        {
            RefUpdates = new [] { branch },
            Commits = new []{ commit },
        }, repoId);
        return push.PushId;
    }

    private List<GitChange> GetInitFiles()
    {
        var result = new List<GitChange>();
        result.Add(new GitChange
        {
            ChangeType = VersionControlChangeType.Add,
            Item = new GitItem
            {
                Path = "/README.md",
            },
            NewContent = new ItemContent
            {
                Content = $"Placeholder for Repository {_repoName} readme file.",
                ContentType = ItemContentType.RawText
            }
        });
        return result;
    }
}

