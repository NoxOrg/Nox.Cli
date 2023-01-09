using Nox.Cli.Actions;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Operations;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsGetProject_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/get-project@v1",
            Author = "Jan Schutte",
            Description = "Get an Azure Devops project",

            Inputs =
            {
                ["connection"] = new NoxActionInput {
                    Id = "connection",
                    Description = "The connection established with action 'azdevops/connect@v1'",
                    Default = new VssConnection(new Uri("https://localhost"), null),
                    IsRequired = true
                },

                ["project-name"] = new NoxActionInput { 
                    Id = "project-name", 
                    Description = "The DevOps project name",
                    Default = string.Empty,
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["project-id"] = new NoxActionOutput {
                    Id = "project-id",
                    Description = "The Id of the Azure devops project",
                },
            }
        };
    }

    private ProjectHttpClient? _projectClient;
    private string? _projectName;

    public async Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectName = inputs.Value<string>("project-name");
        _projectClient = await connection!.GetClientAsync<ProjectHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_projectClient == null || string.IsNullOrEmpty(_projectName))
        {
            ctx.SetErrorMessage("The devops get-project action was not initialized");
        }
        else
        {
            try
            {
                var project = await _projectClient.GetProject(_projectName);
                outputs["project-id"] = project.Id;
            }
            catch
            {
                outputs["project-id"] = null!;
            }
        }
        
        ctx.SetState(ActionState.Success);
        return outputs;
    }

    public Task EndAsync(INoxWorkflowContext ctx)
    {
        _projectClient?.Dispose();
        return Task.CompletedTask;
    }
}

