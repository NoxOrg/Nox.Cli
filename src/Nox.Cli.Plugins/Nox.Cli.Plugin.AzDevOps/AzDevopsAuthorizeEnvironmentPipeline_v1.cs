using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.Plugin.AzDevOps.Clients;

namespace Nox.Cli.Plugin.AzDevOps;

public class AzDevopsAuthorizeEnvironmentPipeline_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/authorize-environment-pipeline@v1",
            Author = "Jan Schutte",
            Description = "Authorize a pipeline to use an environment",

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
                
                ["environment-id"] = new NoxActionInput { 
                    Id = "environment-id", 
                    Description = "The Id (int) of the environment to operate on",
                    Default = 0,
                    IsRequired = true
                },
                ["pipeline-id"] = new NoxActionInput { 
                    Id = "pipeline-id", 
                    Description = "The Id (int) of the DevOps pipeline to authorize",
                    Default = 0,
                    IsRequired = true
                }
            }
        };
    }

    private string? _server;
    private string? _pat;
    private Guid? _projectId;
    private int? _environmentId;
    private int? _pipelineId;
    private bool _isServerContext = false;

    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _server = inputs.Value<string>("server");
        _pat = inputs.Value<string>("personal-access-token");
        _projectId = inputs.Value<Guid>("project-id");
        _pipelineId = inputs.Value<int>("pipeline-id");
        _environmentId = inputs.Value<int>("environment-id");
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
            _environmentId == null ||
            _environmentId == 0 ||
            _pipelineId == null ||
            _pipelineId == 0 )
        {
            ctx.SetErrorMessage("The devops authorize-environment-pipeline action was not initialized");
        }
        else
        {
            try
            {
                var client = new PipelineClient(_server, _pat);
                await client.AuthorizeEnvironmentPipeline(_projectId.Value, _environmentId.Value, _pipelineId.Value);
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