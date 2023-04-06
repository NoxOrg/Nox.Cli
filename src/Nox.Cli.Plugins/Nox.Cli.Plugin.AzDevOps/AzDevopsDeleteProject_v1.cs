using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Operations;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsDeleteProject_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/delete-project@v1",
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
    private OperationsHttpClient? _operationsClient;
    private Guid? _projectId;
    private bool? _isHardDelete;
    private bool _isServerContext;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectId = inputs.Value<Guid>("project-id");
        _isHardDelete = inputs.ValueOrDefault<bool>("hard-delete", this);
        _projectClient = await connection!.GetClientAsync<ProjectHttpClient>();
        _operationsClient = await connection!.GetClientAsync<OperationsHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_projectClient == null || _projectId == null || _projectId == Guid.Empty || _isHardDelete == null)
        {
            ctx.SetErrorMessage("The devops delete-project action was not initialized");
        }
        else
        {
            try
            {
                var operation = await _projectClient.QueueDeleteProject(_projectId.Value, _isHardDelete);

                // Check the operation status every 5 seconds (for up to 30 seconds)
                var completedOperation = await WaitForLongRunningOperation(operation.Id, 5, 30);

                // Check if the operation succeeded (the project was created) or failed
                if (completedOperation.Status == Microsoft.VisualStudio.Services.Operations.OperationStatus.Succeeded)
                {
                    try
                    {
                        // Check if the project has been deleted
                        var project = await _projectClient.GetProject(
                            _projectId.Value.ToString(),
                            includeCapabilities: true,
                            includeHistory: true);
                        ctx.SetErrorMessage("Project was not successfully deleted.");
                    }
                    catch
                    {
                        ctx.SetState(ActionState.Success);
                    }
                }
                else
                {
                    ctx.SetErrorMessage($"Project delete operation failed: {completedOperation.ResultMessage}");
                }
                
                
                
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
        if (!_isServerContext) _projectClient?.Dispose();
        return Task.CompletedTask;
    }
    
    private async Task<Operation> WaitForLongRunningOperation(Guid operationId, int interavalInSec = 5, int maxTimeInSeconds = 60, CancellationToken cancellationToken = default(CancellationToken))
    {
        var expiration = DateTime.Now.AddSeconds(maxTimeInSeconds);

        while (true)
        {
            var operation = await _operationsClient!.GetOperation(operationId, cancellationToken: cancellationToken);

            if (!operation.Completed)
            {
                await Task.Delay(interavalInSec * 1000, cancellationToken);

                if (DateTime.Now > expiration)
                {
                    throw new Exception(String.Format("Operation did not complete in {0} seconds.", maxTimeInSeconds));
                }
            }
            else
            {
                return operation;
            }
        }
    }
    
}

