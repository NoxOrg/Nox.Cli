using Nox.Cli.Actions;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsCreateRepo_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/create-repo@v1",
            Author = "Jan Schutte",
            Description = "Create an Azure Devops repository",

            Inputs =
            {
                ["connection"] = new NoxActionInput {
                    Id = "connection",
                    Description = "The connection established with action 'azdevops/connect@v1'",
                    Default = new VssConnection(new Uri("https://localhost"), null),
                    IsRequired = true
                },
                ["project-name"] = new NoxActionInput { 
                    Id = "project-name", 
                    Description = "The DevOps project name",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["repository-name"] = new NoxActionInput { 
                    Id = "repository-name", 
                    Description = "The DevOps repository name",
                    Default = string.Empty,
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["repository"] = new NoxActionOutput {
                    Id = "repository",
                    Description = "The Azure devops repository",
                },
            }
        };
    }

    private GitHttpClient? _repoClient;
    private string? _repoName;
    private string? _projectName;

    public async Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectName = inputs.Value<string>("project-name");
        _repoName = inputs.Value<string>("repository-name");
        _repoClient = await connection!.GetClientAsync<GitHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_repoClient == null || string.IsNullOrEmpty(_repoName) || string.IsNullOrEmpty(_projectName))
        {
            ctx.SetErrorMessage("The devops create-repo action was not initialized");
        }
        else
        {
            try
            {
                var repo = await _repoClient.GetRepositoryAsync(_projectName, _repoName);
                outputs["repository"] = repo;
                ctx.SetState(ActionState.Success);
            }
            catch
            {
                try
                {
                    //Create the Repo
                    var repo = await CreateRepositoryAsync(ctx);
                    if (repo != null)
                    {
                        outputs["repository"] = repo;
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

    public Task EndAsync(INoxWorkflowContext ctx)
    {
        _repoClient?.Dispose();
        return Task.CompletedTask;
    }
    
    private async Task<GitRepository?> CreateRepositoryAsync(INoxWorkflowContext ctx)
    {
        var repoCreateParameters = new GitRepository()
        {
            Name = _repoName,
        };

        GitRepository repo = null!;
        try
        {
            repo = await _repoClient!.CreateRepositoryAsync(repoCreateParameters, _projectName);
            return repo;
        }
        catch (Exception ex)
        {
            ctx.SetErrorMessage(ex.Message);
        }

        return null;
    }
}

