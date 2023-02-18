using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsGetRepo_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/get-repo@v1",
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
                }
            },

            Outputs =
            {
                ["repository-id"] = new NoxActionOutput {
                    Id = "repository-id",
                    Description = "The Id (Guid) of the Azure devops repository",
                },
            }
        };
    }

    private GitHttpClient? _repoClient;
    private string? _repoName;
    private Guid? _projectId;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectId = inputs.Value<Guid>("project-id");
        _repoName = inputs.Value<string>("repository-name");
        _repoClient = await connection!.GetClientAsync<GitHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_repoClient == null || string.IsNullOrEmpty(_repoName) || _projectId == null || _projectId == Guid.Empty)
        {
            ctx.SetErrorMessage("The devops get-repo action was not initialized");
        }
        else
        {
            try
            {
                var repo = await _repoClient.GetRepositoryAsync(_projectId!.Value, _repoName);
                outputs["repository-id"] = repo.Id;
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
        _repoClient?.Dispose();
        return Task.CompletedTask;
    }
}

