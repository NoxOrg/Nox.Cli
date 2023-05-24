using Microsoft.VisualStudio.Services.ServiceEndpoints;
using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.AzDevOps;

public class AzDevOpsShareServiceEndpoint_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/share-service-endpoint@v1",
            Author = "Jan Schutte",
            Description = "Share an Azure Devops Service Endpoint",

            Inputs =
            {
                ["connection"] = new NoxActionInput {
                    Id = "connection",
                    Description = "The connection established with action 'azdevops/connect@v1'",
                    Default = new VssConnection(new Uri("https://localhost"), null),
                    IsRequired = true
                },
                ["service-endpoint-id"] = new NoxActionInput { 
                    Id = "service-endpoint-id", 
                    Description = "The identifier of the DevOps Service Endpoint to share with the specified project.",
                    Default = Guid.Empty,
                    IsRequired = true
                },
                ["service-endpoint-name"] = new NoxActionInput { 
                    Id = "service-endpoint-name", 
                    Description = "The name of the shared service endpoint in the target project",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["project-id"] = new NoxActionInput { 
                    Id = "project-id", 
                    Description = "The DevOps project Identifier of the target project that will get the shared endpoint",
                    Default = Guid.Empty,
                    IsRequired = true
                },
                ["project-name"] = new NoxActionInput { 
                    Id = "project-name", 
                    Description = "The DevOps project name of the target project that will get the shared endpoint",
                    Default = string.Empty,
                    IsRequired = true
                }
            }
        };
    }

    private ServiceEndpointHttpClient? _seClient;
    private Guid? _seId;
    private string? _seName;
    private Guid? _projectId;
    private string? _projectName;
    private bool _isServerContext = false;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _seId = inputs.Value<Guid>("service-endpoint-id");
        _seName = inputs.Value<string>("service-endpoint-name");
        _projectId = inputs.Value<Guid>("project-id");
        _projectName = inputs.Value<string>("project-name");
        _seClient = await connection!.GetClientAsync<ServiceEndpointHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_seClient == null ||
            _seId == null ||
            _seId == Guid.Empty ||
            string.IsNullOrEmpty(_seName) ||
            _projectId == null ||
            _projectId == Guid.Empty ||
            string.IsNullOrEmpty(_projectName))
        {
            ctx.SetErrorMessage("The devops share-service-endpoint action was not initialized");
        }
        else
        {
            try
            {
                var seProjectReference = new ServiceEndpointProjectReference
                {
                    Name = _seName!,
                    ProjectReference = new ProjectReference
                    {
                        Id = _projectId.Value,
                        Name = _projectName!
                    }
                };
                await _seClient.ShareServiceEndpointAsync(_seId.Value, new ServiceEndpointProjectReference[] { seProjectReference });  
                
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
        if (!_isServerContext && _seClient != null) _seClient.Dispose();
        return Task.CompletedTask;
    }
}