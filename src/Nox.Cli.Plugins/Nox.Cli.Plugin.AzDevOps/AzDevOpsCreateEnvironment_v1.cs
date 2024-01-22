using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.AzDevOps;

public class AzDevOpsCreateEnvironment_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/create-environment@v1",
            Author = "Jan Schutte",
            Description = "Create an Azure Devops pipeline environment",

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
                
                ["environment-name"] = new NoxActionInput { 
                    Id = "environment-name", 
                    Description = "The name of the environment to find.",
                    Default = string.Empty,
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["environment-id"] = new NoxActionOutput {
                    Id = "environment-id",
                    Description = "The Id of the Azure devops environment. Will return null if it does not exist.",
                },
            }
        };
    }

    private TaskAgentHttpClient? _agentClient;
    private Guid? _projectId;
    private string? _envName;
    private bool _isServerContext = false;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectId = inputs.Value<Guid>("project-id");
        _envName = inputs.Value<string>("environment-name");
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
            string.IsNullOrEmpty(_envName))
        {
            ctx.SetErrorMessage("The devops find-environment action was not initialized");
        }
        else
        {
            try
            {
                var envInstance = await _agentClient.AddEnvironmentAsync(_projectId.Value, new EnvironmentCreateParameter
                {
                    Name = _envName,
                    Description = $"The {_envName} environment"
                });
                outputs["environment-id"] = envInstance.Id;
                
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
        if (!_isServerContext && _agentClient != null) _agentClient.Dispose();
        return Task.CompletedTask;
    }
}