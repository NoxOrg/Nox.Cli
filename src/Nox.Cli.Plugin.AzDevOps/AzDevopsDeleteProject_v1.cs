using Nox.Cli.Actions;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Operations;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsDeleteProject_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/delete-Project@v1",
            Author = "Jan Schutte",
            Description = "Delete an Azure Devops project",

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
                
                ["hard-delete"] = new NoxActionInput { 
                    Id = "hard-delete", 
                    Description = "Indicate if the project should be hard deleted.",
                    Default = false,
                    IsRequired = false
                },
            }
        };
    }

    private ProjectHttpClient? _projectClient;
    private Guid? _projectId;
    private bool? _isHardDelete;

    public async Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string,object> inputs)
    {
        var connection = (VssConnection)inputs["connection"];
        _projectId = inputs.Value<Guid?>("project-id");
        _projectClient = await connection.GetClientAsync<ProjectHttpClient>();
        _isHardDelete = inputs.ValueOrDefault<bool?>("hard-delete", this);
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_projectClient == null || _projectId == null || _isHardDelete == null)
        {
            ctx.SetErrorMessage("The devops delete-project action was not initialized");
        }
        else
        {
            try
            {
                var project = await _projectClient.QueueDeleteProject(_projectId.Value, _isHardDelete);
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
        _projectClient?.Dispose();
        return Task.CompletedTask;
    }
    
}

