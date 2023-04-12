using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsFindRepo_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/delete-repo@v1",
            Author = "Jan Schutte",
            Description = "Find an Azure Devops repository in a project",

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
                    Description = "The project Id (Guid) of the devops project to delete ",
                    Default = Guid.Empty,
                    IsRequired = true
                },
                
                ["repository-name"] = new NoxActionInput { 
                    Id = "repository-name", 
                    Description = "The name of the repository to delete ",
                    Default = string.Empty,
                    IsRequired = true
                }
            },

            Outputs =
            {
                ["is-found"] = new NoxActionOutput {
                    Id =  "is-found",
                    Description = "Indicates if the repository exists"
                },
                
                ["repository-id"] = new NoxActionOutput {
                    Id = "repository-id",
                    Description = "The Id of the Azure devops repository. Will return null if it does not exist.",
                },
            }
        };
    }

    private GitHttpClient? _gitClient;
    private Guid? _projectId;
    private string? _repoName;
    private bool _isServerContext;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectId = inputs.Value<Guid>("project-id");
        _repoName = inputs.Value<string>("repository-name");
        _gitClient = await connection!.GetClientAsync<GitHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_gitClient == null || 
            _projectId == null || 
            _projectId == Guid.Empty || 
            string.IsNullOrEmpty(_repoName))
        {
            ctx.SetErrorMessage("The devops find-repo action was not initialized");
        }
        else
        {
            try
            {
                outputs["is-found"] = false;
                var repo = await _gitClient.GetRepositoryAsync(_projectId.Value, _repoName);
                if(repo != null)
                {
                    outputs["is-found"] = true;
                    outputs["repository-id"] = repo.Id;
                }
            }
            catch 
            {
                //Ignore, gets exception when repo does not exist.
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