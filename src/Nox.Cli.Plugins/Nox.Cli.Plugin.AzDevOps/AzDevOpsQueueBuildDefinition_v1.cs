using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevOpsQueueBuildDefinition_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/queue-build-definition@v1",
            Author = "Jan Schutte",
            Description = "Queue an Azure Devops Build Definition for execution.",

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
                
                ["build-definition-id"] = new NoxActionInput { 
                    Id = "build-definition-id", 
                    Description = "The DevOps Build Definition Identifier",
                    Default = 0,
                    IsRequired = true
                }
            }
        };
    }

    private BuildHttpClient? _buildClient;
    private ProjectHttpClient? _projectClient;
    private Guid? _projectId;
    private int? _buildId;
    private bool _isServerContext = false;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectId = inputs.Value<Guid>("project-id");
        _buildId = inputs.Value<int>("build-definition-id");
        _buildClient = await connection!.GetClientAsync<BuildHttpClient>();
        _projectClient = await connection!.GetClientAsync<ProjectHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_buildClient == null ||
            _projectClient == null ||
            _projectId == null ||
            _projectId == Guid.Empty ||
            _buildId is null or 0)
        {
            ctx.SetErrorMessage("The devops queue-build-definition action was not initialized");
        }
        else
        {
            try
            {
                var project = await _projectClient.GetProject(_projectId.Value.ToString());
                if (project == null)
                {
                    ctx.SetErrorMessage("Unable to find project in DevOps");
                }
                else
                {
                    var buildDefinition = await _buildClient.GetDefinitionAsync(_projectId.Value, _buildId.Value);
                    if (buildDefinition != null)
                    {
                        var build = new Build
                        {
                            Definition = buildDefinition,
                            Project = project
                        };
                        await _buildClient.QueueBuildAsync(build);
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
        if (!_isServerContext && _buildClient != null) _buildClient.Dispose();
        return Task.CompletedTask;
    }
}