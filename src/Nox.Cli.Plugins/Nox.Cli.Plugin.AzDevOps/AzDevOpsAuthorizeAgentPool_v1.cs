using System.Text.Json;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.Abstractions.Helpers;
using Nox.Cli.Plugin.AzDevOps.Clients;
using Nox.Cli.Plugin.AzDevOps.DTO;
using RestSharp;

namespace Nox.Cli.Plugin.AzDevOps;

public class AzDevOpsAuthorizeAgentPool_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/authorize-agent-pool@v1",
            Author = "Jan Schutte",
            Description = "Authorize all pipelines in a project to use an agent pool",

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
                
                ["agent-pool-queue-id"] = new NoxActionInput { 
                    Id = "agent-pool-queue-id", 
                    Description = "The Id (int) of the Agent Pool Queue",
                    Default = 0,
                    IsRequired = true
                }
            }
        };
    }

    private string? _server;
    private string? _pat;
    private Guid? _projectId;
    private int? _queueId;
    private bool _isServerContext = false;

    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _server = inputs.Value<string>("server");
        _pat = inputs.Value<string>("personal-access-token");
        _projectId = inputs.Value<Guid>("project-id");
        _queueId = inputs.Value<int>("agent-pool-queue-id");
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
            _queueId == 0 ||
            string.IsNullOrWhiteSpace(_pat))
        {
            ctx.SetErrorMessage("The DevOps authorize-agent-pool action was not initialized");
        }
        else
        {
            try
            {
                var client = new PipelineClient(_server, _pat);
                await client.AuthorizeAgentQueuePipelines(_projectId.Value, _queueId!.Value);
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