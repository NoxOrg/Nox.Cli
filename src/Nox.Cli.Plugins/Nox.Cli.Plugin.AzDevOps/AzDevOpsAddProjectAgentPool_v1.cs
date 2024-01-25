using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.AzDevOps;

public class AzDevOpsAddProjectAgentPool_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/add-project-agent-pool-queue@v1",
            Author = "Jan Schutte",
            Description = "Add an agent pool queue to an Azure DevOps agent pool",

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
                
                ["agent-pool-name"] = new NoxActionInput { 
                    Id = "agent-pool-name", 
                    Description = "The name of the agent pool to add to the project.",
                    Default = string.Empty,
                    IsRequired = true
                },
            },
            
            Outputs =
            {
                ["agent-pool-queue-id"] = new NoxActionOutput {
                    Id = "agent-pool-queue-id",
                    Description = "The agent pool queue id of the added agent pool",
                },
            }
        };
    }
    
    private TaskAgentHttpClient? _agentClient;
    private Guid? _projectId;
    private string? _poolName;
    private bool _isServerContext = false;

    public async Task BeginAsync(IDictionary<string, object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectId = inputs.Value<Guid>("project-id");
        _poolName = inputs.Value<string>("agent-pool-name");
        _agentClient = await connection!.GetClientAsync<TaskAgentHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_agentClient == null ||
            _projectId == null ||
            _projectId == Guid.Empty ||
            string.IsNullOrEmpty(_poolName))
        {
            ctx.SetErrorMessage("The devops add-project-agent-pool action was not initialized");
        }
        else
        {
            try
            {
                var pools = await _agentClient.GetAgentPoolsAsync(_poolName);
                if (pools == null)
                {
                    ctx.SetErrorMessage($"Unable to locate Agent Pool: {_poolName}");
                }
                else
                {
                    var agentQueues = await _agentClient.GetAgentQueuesAsync(_projectId.Value, _poolName);
                    if (agentQueues == null || agentQueues.Count == 0)
                    {
                        var agentQueue = await _agentClient.AddAgentQueueAsync(_projectId.Value, new TaskAgentQueue
                        {
                            Name = _poolName,
                            Pool = pools[0],
                            ProjectId = _projectId.Value,
                        });
                        outputs["agent-pool-queue-id"] = agentQueue.Id;
                    }
                    else
                    {
                        outputs["agent-pool-queue-id"] = agentQueues[0].Id;
                    }
                    ctx.SetState(ActionState.Success);
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
        if (!_isServerContext && _agentClient != null) _agentClient.Dispose();
        return Task.CompletedTask;
    }
}