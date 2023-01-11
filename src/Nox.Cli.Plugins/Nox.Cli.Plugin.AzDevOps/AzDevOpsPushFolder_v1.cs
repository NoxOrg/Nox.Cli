using Nox.Cli.Actions;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
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
                ["commit-path"] = new NoxActionInput
                {
                    Id = "commit-path", 
                    Description = "The local path to commit to the repository (All files and folders will be added and committed)",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["branch-name"] = new NoxActionInput
                {
                    Id = "branch-name", 
                    Description = "The name of the branch to which to commit",
                    Default = "",
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
    private string? _commitPath;
    private Guid? _repoId;
    private string? _branchName;

    public async Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _repoId = inputs.Value<Guid>("repository-id");
        _commitPath = inputs.Value<string>("commit-path");
        _gitClient = await connection!.GetClientAsync<GitHttpClient>();
        _branchName = inputs.Value<string>("branch-name");
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_gitClient == null || _repoId == null || _repoId == Guid.Empty || string.IsNullOrEmpty(_commitPath) || string.IsNullOrEmpty(_branchName))
        {
            ctx.SetErrorMessage("The devops commit-folder action was not initialized");
        }
        else
        {
            try
            {
                var commit = CreateCommit();
                var branch = new GitRefUpdate
                {
                    Name = $"ref/heads/{_branchName}",
                    RepositoryId = _repoId!.Value,
                    NewObjectId = "0000000000000000000000000000000000000000"
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

    public Task EndAsync(INoxWorkflowContext ctx)
    {
        _gitClient?.Dispose();
        return Task.CompletedTask;
    }

    private GitCommitRef CreateCommit()
    {
        var changes = new List<GitChange>();
        var di = new DirectoryInfo(_commitPath);
        
        

        foreach (var file in di.GetFiles())
        {
            //Add the file
        }

        foreach (var dir in di.GetDirectories())
        {
            
        }
        var result = new GitCommitRef
        {
            Comment = "Initial Commit",
            Changes = changes
        };
        return result;
    }

    private List<GitChange> GetFolderChanged()
    {
        var result = new List<GitChange>();
        foreach (var dir in Directory.GetDirectories(_commitPath))
        {
            foreach (var file in Directory.GetFiles(dir))
            {
                result.Add(new GitChange
                {
                    ChangeType = VersionControlChangeType.Add,
                    Item = new GitItem
                    {
                        Path = $"/{_branchName}/{file}"
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

