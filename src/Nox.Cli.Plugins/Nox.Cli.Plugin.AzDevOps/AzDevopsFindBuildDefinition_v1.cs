using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsFindBuildDefinition_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/find-build-definition@v1",
            Author = "Jan Schutte",
            Description = "Find an Azure Devops Build Definition",

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
                
                ["build-name"] = new NoxActionInput { 
                    Id = "project-name", 
                    Description = "The DevOps build definition name",
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
                
                ["build-definition-id"] = new NoxActionOutput {
                    Id = "build-id",
                    Description = "The Id of the Azure devops build. Will return null if it does not exist.",
                },
            }
        };
    }

    private BuildHttpClient? _buildClient;
    private Guid? _projectId;
    private string? _buildName;
    private bool _isServerContext = false;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectId = inputs.Value<Guid>("project-id");
        _buildName = inputs.Value<string>("build-name");
        _buildClient = await connection!.GetClientAsync<BuildHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_buildClient == null ||
            _projectId == null ||
            _projectId == Guid.Empty ||
            string.IsNullOrEmpty(_buildName))
        {
            ctx.SetErrorMessage("The devops find-build action was not initialized");
        }
        else
        {
            try
            {
                var buildDefinitions = await _buildClient.GetDefinitionsAsync(_projectId.Value);
                var build = buildDefinitions.FirstOrDefault(bd => String.Equals(bd.Name, _buildName, StringComparison.OrdinalIgnoreCase));
                outputs["is-found"] = false;
                if (build != null)
                {
                    outputs["is-found"] = true;
                    outputs["build-definition-id"] = build.Id;
                }
                //outputs["build-id"] = build.ToImmutableDictionary();
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
        if (!_isServerContext && _buildClient != null) _buildClient.Dispose();
        return Task.CompletedTask;
    }
}