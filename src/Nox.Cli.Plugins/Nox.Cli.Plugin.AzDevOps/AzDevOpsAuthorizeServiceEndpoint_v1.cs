using System.Text.Json;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.Abstractions.Helpers;
using Nox.Cli.Plugin.AzDevOps.Clients;
using Nox.Cli.Plugin.AzDevOps.DTO;
using RestSharp;

namespace Nox.Cli.Plugin.AzDevOps;

public class AzDevOpsAuthorizeServiceEndpoint_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/authorize-service-endpoint@v1",
            Author = "Jan Schutte",
            Description = "Authorize a DevOps pipeline to use a service endpoint",

            Inputs =
            {
                ["server"] = new NoxActionInput { 
                    Id = "server", 
                    Description = "The DevOps server hostname or IP",
                    Default = "localhost",
                    IsRequired = true
                },
                
                ["personal-access-token"] = new NoxActionInput {
                    Id = "personal-access-token",
                    Description = "The personal access token to connect to DevOps with",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["project-id"] = new NoxActionInput { 
                    Id = "project-id", 
                    Description = "The project Id (Guid) of the devops project",
                    Default = Guid.Empty,
                    IsRequired = true
                },
                
                ["service-endpoint-id"] = new NoxActionInput { 
                    Id = "service-endpoint-id", 
                    Description = "The Id of the Service Endpoint to authorize",
                    Default = Guid.Empty,
                    IsRequired = true
                },
                ["pipeline-id"] = new NoxActionInput { 
                    Id = "pipeline-id", 
                    Description = "The Id (int) of the DevOps pipeline",
                    Default = 0,
                    IsRequired = true
                }
                
            }
        };
    }

    private string? _server;
    private string? _pat;
    private Guid? _projectId;
    private Guid? _endpointId;
    private int? _pipelineId;
    private bool _isServerContext = false;

    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _server = inputs.Value<string>("server");
        _pat = inputs.Value<string>("personal-access-token");
        _projectId = inputs.Value<Guid>("project-id");
        _endpointId = inputs.Value<Guid>("service-endpoint-id");
        _pipelineId = inputs.Value<int>("pipeline-id");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrWhiteSpace(_server) ||
            string.IsNullOrWhiteSpace(_pat) ||
            _projectId == null ||
            _projectId == Guid.Empty ||
            _endpointId == null ||
            _endpointId == Guid.Empty ||
            _pipelineId == 0 ||
            string.IsNullOrWhiteSpace(_pat))
        {
            ctx.SetErrorMessage("The DevOps authorize-service-endpoint action was not initialized");
        }
        else
        {
            try
            {
                var client = new PipelineClient(_server, _pat);
                await client.AuthorizeEndpointPipeline(_projectId.Value, _endpointId.Value, _pipelineId!.Value);
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
        return Task.CompletedTask;
    }
}