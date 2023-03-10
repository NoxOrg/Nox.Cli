using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevOpsPushFolder_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/commit-folder@v1",
            Author = "Jan Schutte",
            Description = "Commit a local folder to a Azure Devops repository",

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
                ["source-path"] = new NoxActionInput
                {
                    Id = "source-path", 
                    Description = "The local path to commit to the repository (All files and folders will be added and committed)",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["branch-name"] = new NoxActionInput
                {
                    Id = "branch-name", 
                    Description = "The name of the branch to which to commit",
                    Default = "main",
                    IsRequired = true
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

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _repoId = inputs.Value<Guid>("repository-id");
        _sourcePath = inputs.Value<string>("source-path");
        _gitClient = await connection!.GetClientAsync<GitHttpClient>();
        _branchName = inputs.ValueOrDefault<string>("branch-name", this);
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_gitClient == null || _repoId == null || _repoId == Guid.Empty || string.IsNullOrEmpty(_sourcePath) || string.IsNullOrEmpty(_branchName))
        {
            ctx.SetErrorMessage("The devops push-folder action was not initialized");
        }
        else
        {
            try
            {
                var commit = CreateCommit();
                var branch = new GitRefUpdate
                {
                    Name = $"refs/heads/{_branchName}",
                    RepositoryId = _repoId!.Value,
                    OldObjectId = "0000000000000000000000000000000000000000"
                };
                    
                var push = await _gitClient.CreatePushAsync(new GitPush
                {
                    RefUpdates = new [] { branch },
                    Commits = new []{ commit },
                }, _repoId!.Value);
                outputs["commit-id"] = push.PushId;
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
        _gitClient?.Dispose();
        return Task.CompletedTask;
    }

    private GitCommitRef CreateCommit()
    {
        var changes = GetFolderChanges(_sourcePath!);
        var result = new GitCommitRef
        {
            Comment = "Initial Commit",
            Changes = changes
        };
        return result;
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
                result.Add(new GitChange
                {
                    ChangeType = VersionControlChangeType.Add,
                    Item = new GitItem
                    {
                        Path = $"{relativePath}/{Path.GetFileName(file)}"
                    },
                    NewContent = new ItemContent
                    {
                        Content = File.ReadAllText(file),
                        ContentType = ItemContentType.RawText
                    }
                });
            }    
        }
        

        return result;
    }
}

