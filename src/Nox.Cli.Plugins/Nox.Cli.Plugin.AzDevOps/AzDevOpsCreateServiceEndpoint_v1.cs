using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.AzDevOps;

public class AzDevOpsCreateServiceEndpoint_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/create-service-endpoint@v1",
            Author = "Jan Schutte",
            Description = "Create an Azure Devops Service Endpoint",

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
                
                ["name"] = new NoxActionInput { 
                    Id = "name", 
                    Description = "The DevOps Service Endpoint name",
                    Default = string.Empty,
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["service-endpoint-id"] = new NoxActionOutput {
                    Id = "service-endpoint-id",
                    Description = "The Id of the Azure devops service endpoint.",
                },
            }
        };
    }

    private ServiceEndpointHttpClient? _seClient;
    private Guid? _projectId;
    private string? _name;
    private bool _isServerContext = false;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectId = inputs.Value<Guid>("project-id");
        _name = inputs.Value<string>("name");
        _seClient = await connection!.GetClientAsync<ServiceEndpointHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_seClient == null ||
            _projectId == null ||
            _projectId == Guid.Empty ||
            string.IsNullOrEmpty(_name))
        {
            ctx.SetErrorMessage("The devops create-service-endpoint action was not initialized");
        }
        else
        {
            try
            {
                var serviceEndpoints = await _seClient.GetServiceEndpointsAsync(_projectId.Value);
                var serviceEndpoint = serviceEndpoints.FirstOrDefault(se => se.Name == _name);
                if (serviceEndpoint != null)
                {
                    ctx.SetErrorMessage("This service endpoint already exists. Duplicates are not allowed!");
                }
                else
                {
                    serviceEndpoint = new ServiceEndpoint
                    {
                        Name = _name,
                        
                    };
                    var result = await _seClient.CreateServiceEndpointAsync(serviceEndpoint);
                    outputs["service-endpoint-id"] = result.Id;
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
        if (!_isServerContext && _seClient != null) _seClient.Dispose();
        return Task.CompletedTask;
    }
}