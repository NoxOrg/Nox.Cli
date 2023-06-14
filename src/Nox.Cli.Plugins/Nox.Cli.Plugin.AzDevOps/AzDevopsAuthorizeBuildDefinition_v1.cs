using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.AzDevOps;

public class AzDevopsAuthorizeBuildDefinition_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/authorize-build-definition@v1",
            Author = "Jan Schutte",
            Description = "Authorize all unauthorized service endpoints on an Azure Devops Build Definition",

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
                },
            }
        };
    }

    private BuildHttpClient? _buildClient;
    private Guid? _projectId;
    private int? _buildId;
    private bool _isServerContext = false;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectId = inputs.Value<Guid>("project-id");
        _buildId = inputs.Value<int>("build-definition-id");
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
            _buildId == null)
        {
            ctx.SetErrorMessage("The devops authorize-build-definition action was not initialized");
        }
        else
        {
            try
            {
                var build = await _buildClient.GetDefinitionAsync(_projectId.Value, _buildId.Value);
                if (build == null)
                {
                    ctx.SetErrorMessage("Unable to find the specified build definition");
                }
                else
                {
                    var resources = await _buildClient.GetDefinitionResourcesAsync(_projectId.Value, _buildId.Value);
                    var unAuths = resources.Where(r => !r.Authorized).ToArray();
                    if (unAuths.Any())
                    {
                        foreach (var unAuth in unAuths)
                        {
                            unAuth.Authorized = true;
                        }

                        await _buildClient.AuthorizeDefinitionResourcesAsync(unAuths, _projectId.Value, _buildId.Value);
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