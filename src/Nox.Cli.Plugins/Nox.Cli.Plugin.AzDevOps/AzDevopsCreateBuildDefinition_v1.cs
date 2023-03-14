using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Core.Exceptions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsCreateBuildDefinition_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/clone-build-definition@v1",
            Author = "Jan Schutte",
            Description = "Create an Azure Devops Build Definition from a json build definition file",

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
                ["repository-id"] = new NoxActionInput {
                    Id = "repository-id",
                    Description = "The Id (Guid) of the devops repository. Normally the output from 'azdevops/get-repo@v1'",
                    Default = Guid.Empty,
                    IsRequired = true
                },
                ["source-branch"] = new NoxActionInput { 
                    Id = "source-branch", 
                    Description = "The name of the default branch in the repository",
                    Default = "main",
                    IsRequired = true
                },
                ["yaml-file-path"] = new NoxActionInput { 
                    Id = "yaml-file-path", 
                    Description = "The path to the yaml file containing the build definition",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["build-name"] = new NoxActionInput { 
                    Id = "build-name", 
                    Description = "The name of the build definition to create",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["agent-pool"] = new NoxActionInput { 
                    Id = "agent-pool", 
                    Description = "The name of the Agent Pool to use for this build definition",
                    Default = string.Empty,
                    IsRequired = true
                }
            }
        };
    }

    private BuildHttpClient? _buildClient;
    private GitHttpClient? _repoClient;
    private Guid? _projectId;
    private Guid? _repoId;
    private string? _branchName;
    private string? _yamlFilePath;
    private string? _buildName;
    private string? _agentPool;
    private bool _isServerContext = false;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectId = inputs.Value<Guid>("project-id");
        _repoId = inputs.Value<Guid>("repository-id");
        _branchName = inputs.ValueOrDefault<string>("source-branch", this);
        _yamlFilePath = inputs.Value<string>("yaml-file-path");
        _buildName = inputs.Value<string>("build-name");
        _agentPool = inputs.Value<string>("agent-pool");
        _buildClient = await connection!.GetClientAsync<BuildHttpClient>();
        _repoClient = await connection!.GetClientAsync<GitHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_buildClient == null ||
            _repoClient == null ||
            _projectId == null ||
            _projectId == Guid.Empty ||
            _repoId == Guid.Empty ||
            string.IsNullOrEmpty(_branchName) ||
            string.IsNullOrEmpty(_yamlFilePath) ||
            string.IsNullOrEmpty(_buildName) ||
            string.IsNullOrEmpty(_agentPool))
        {
            ctx.SetErrorMessage("The devops clone-build-definition action was not initialized");
        }
        else
        {
            try
            {
                var repo = await _repoClient.GetRepositoryAsync(_repoId!.Value.ToString());
                if (repo == null) throw new NoxException("Unable to locate the source repository!");
                
                var ymlProcess = new YamlProcess
                {
                    YamlFilename = _yamlFilePath
                };
                var newBuild = new BuildDefinition
                {
                    Process = ymlProcess,
                    Repository = new BuildRepository
                    {
                        Name = repo.Name,
                        DefaultBranch = _branchName,
                        Url = new Uri(repo.Url),
                        Type = RepositoryTypes.TfsGit
                    },
                    Name = repo.Name,
                    Queue = new AgentPoolQueue
                    {
                        Name = _agentPool
                    },
                };
                var ciTrigger = new ContinuousIntegrationTrigger();
                ciTrigger.BranchFilters.Add(_branchName);
                
                newBuild.Triggers.Add(ciTrigger);
                
                await _buildClient.CreateDefinitionAsync(newBuild, _projectId.Value);
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
        if (!_isServerContext && _repoClient != null) _repoClient.Dispose();
        return Task.CompletedTask;
    }
}