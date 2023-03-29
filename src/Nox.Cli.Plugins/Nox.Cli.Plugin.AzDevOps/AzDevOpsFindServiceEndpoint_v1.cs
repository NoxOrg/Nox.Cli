using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevOpsFindServiceEndpoint_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/find-service-endpoint@v1",
            Author = "Jan Schutte",
            Description = "Find an Azure Devops Service Endpoint",

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
                
                ["service-endpoint-name"] = new NoxActionInput { 
                    Id = "service-endpoint-name", 
                    Description = "The DevOps Service Endpoint name to find",
                    Default = string.Empty,
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["is-found"] = new NoxActionOutput {
                    Id =  "is-found",
                    Description = "Indicates if the build definition exists"
                },
                
                ["service-endpoint-id"] = new NoxActionOutput {
                    Id = "service-endpoint-id",
                    Description = "The Id of the Azure devops service endpoint. Will return null if it does not exist.",
                },
            }
        };
    }

    private ServiceEndpointHttpClient? _seClient;
    private Guid? _projectId;
    private string? _serviceEndpointName;
    private bool _isServerContext = false;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectId = inputs.Value<Guid>("project-id");
        _serviceEndpointName = inputs.Value<string>("service-endpoint-name");
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
            string.IsNullOrEmpty(_serviceEndpointName))
        {
            ctx.SetErrorMessage("The devops find-service-endpoint action was not initialized");
        }
        else
        {
            try
            {
                
                var serviceEndpoints = await _seClient.GetServiceEndpointsAsync(_projectId.Value);
                var serviceEndpoint = serviceEndpoints.FirstOrDefault(se => se.Name == _serviceEndpointName);
                if (serviceEndpoint != null)
                {
                    outputs["is-found"] = true;
                    outputs["service-endpoint-id"] = serviceEndpoint.Id;
                }
                else
                {
                    outputs["is-found"] = false;
                }
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