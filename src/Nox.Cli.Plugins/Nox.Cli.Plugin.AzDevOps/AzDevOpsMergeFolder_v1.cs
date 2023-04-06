using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.Plugins.AzDevops.Helpers;
using Nox.Core.Exceptions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevOpsMergeFolder_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/merge-folder@v1",
            Author = "Jan Schutte",
            Description = "Merge a local folder to a Azure Devops repository",

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
                    Description = "The DevOps repository Identifier",
                    Default = Guid.Empty,
                    IsRequired = true
                },
                ["branch-name"] = new NoxActionInput { 
                    Id = "branch-name", 
                    Description = "The name of the branch to push and merge into main",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["source-path"] = new NoxActionInput
                {
                    Id = "source-path", 
                    Description = "The local path to commit to the repository (All files and folders will be added and committed)",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["reference-date-time"] = new NoxActionInput
                {
                    Id = "reference-date-time",
                    Description = "This date time will be used to compare with files in the repo in order to determine which files are edits and which files are new additions. Typically the download-date-time output from azdevops/download-repo-branch@v1",
                    Default = DateTime.Now,
                    IsRequired = false
                },
                ["auto-complete"] = new NoxActionInput
                {
                    Id = "auto-complete",
                    Description = "If this flag is set to true the Pull Request will be mark as completed.",
                    Default = true,
                    IsRequired = false
                }
            },

            Outputs =
            {
                ["commit-id"] = new NoxActionOutput {
                    Id = "commit-id",
                    Description = "The Id (Guid) of the Azure devops Commit",
                },
            }
        };
    }

    private GitHttpClient? _gitClient;
    private string? _sourcePath;
    private Guid? _repoId;
    private string? _branchName;
    private DateTime? _refDateTime;
    private bool? _autoComplete;
    private bool _isServerContext = false;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _repoId = inputs.Value<Guid>("repository-id");
        _sourcePath = inputs.Value<string>("source-path");
        _branchName = inputs.Value<string>("branch-name");
        _refDateTime = inputs.ValueOrDefault<DateTime>("reference-date-time", this);
        _autoComplete = inputs.ValueOrDefault<bool>("auto-complete", this);
        _gitClient = await connection!.GetClientAsync<GitHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_gitClient == null || 
            _repoId == null || 
            _repoId == Guid.Empty || 
            _refDateTime == null ||
            string.IsNullOrEmpty(_sourcePath) ||
            string.IsNullOrEmpty(_branchName) ||
            _autoComplete == null)
        {
            ctx.SetErrorMessage("The devops merge-folder action was not initialized");
        }
        else
        {
            try
            {
                var commit = CreateCommit();
                var pushId = await CreatePush(_branchName, commit);
                var pr = await CreatePullRequest(_branchName);
                Thread.Sleep(TimeSpan.FromSeconds(5));
                if (_autoComplete == true) await CompletePullRequest(pr);
                outputs["commit-id"] = pushId;
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
        if (!_isServerContext) _gitClient?.Dispose();
        return Task.CompletedTask;
    }

    private GitCommitRef CreateCommit()
    {
        var changes = GetFolderChanges(_sourcePath!);
        var result = new GitCommitRef
        {
            Comment = $"Nox Cli commit {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            Changes = changes
        };
        return result;
    }

    private async Task<int> CreatePush(string branchName, GitCommitRef commit)
    {
        var serverBranches = await _gitClient!.GetRefsAsync(_repoId!.Value.ToString(), filter: $"heads/{branchName}");
        if (serverBranches.Count != 1) throw new NoxException($"Unable to locate branch {branchName}");
        
        var branch = new GitRefUpdate
        {
            Name = $"refs/heads/{branchName}",
            RepositoryId = _repoId!.Value,
            OldObjectId = serverBranches[0].ObjectId,
        };
                    
        var push = await _gitClient.CreatePushAsync(new GitPush
        {
            RefUpdates = new [] { branch },
            Commits = new []{ commit },
        }, _repoId!.Value);
        return push.PushId;
    }

    private async Task<GitPullRequest> CreatePullRequest(string branchName)
    {
        var pr = await _gitClient!.CreatePullRequestAsync(new GitPullRequest
        {
            SourceRefName = $"refs/heads/{branchName}",
            TargetRefName = "refs/heads/main",
            Title = "[Nox Cli Auto Merge]"
        }, _repoId!.Value);
        return pr!;
    }

    private async Task CompletePullRequest(GitPullRequest pr)
    {
        var completionOptions = new GitPullRequestCompletionOptions
        {
            DeleteSourceBranch = true,
            BypassPolicy = true,
            SquashMerge = true,
        };
        var mergeRequest = new GitPullRequest
        {
            Status = PullRequestStatus.Completed,
            CompletionOptions = completionOptions,
            LastMergeSourceCommit = pr.LastMergeSourceCommit
        };
        await _gitClient!.UpdatePullRequestAsync(mergeRequest, _repoId!.Value, pr.PullRequestId);
    }
    private List<GitChange> GetFolderChanges(string path, string root = "")
    {
        if (string.IsNullOrEmpty(root)) root = path;
        var result = new List<GitChange>();
        var fileChanges = GetFileChanges(path, root);
        if (fileChanges.Any()) result.AddRange(GetFileChanges(path, root));
        
        foreach (var dir in Directory.GetDirectories(path))
        {
            result.AddRange(GetFolderChanges(dir, root));
        }
        return result;
    }

    private List<GitChange> GetFileChanges(string path, string root)
    {
        var result = new List<GitChange>();
        var files = Directory.GetFiles(path);
        var relativePath = path.Remove(0, root.Length);
        if (files.Length > 0)
        {
            foreach (var file in files)
            {
                ItemContent itemContent;
                var fileExt = Path.GetExtension(file);
                if (FileExtensionHelper.IsBinaryFile(fileExt))
                {
                    var fileContent = File.ReadAllBytes(file);
                    itemContent = new ItemContent
                    {
                        Content = Convert.ToBase64String(fileContent),
                        ContentType = ItemContentType.Base64Encoded
                    };
                }
                else
                {
                    itemContent = new ItemContent
                    {
                        Content = File.ReadAllText(file),
                        ContentType = ItemContentType.RawText
                    };
                }
                
                var changeType = VersionControlChangeType.Edit;
                //If the file was created in the last minute it has to be an Add
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime > _refDateTime) changeType = VersionControlChangeType.Add;
                result.Add(new GitChange
                {
                    ChangeType = changeType,
                    Item = new GitItem
                    {
                        Path = $"{relativePath}/{Path.GetFileName(file)}"
                    },
                    NewContent = itemContent
                });
            }    
        }
        

        return result;
    }
}