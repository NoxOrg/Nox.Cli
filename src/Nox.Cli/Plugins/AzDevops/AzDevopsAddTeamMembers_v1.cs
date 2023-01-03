using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Actions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsAddTeamMembers_v1 : NoxAction
{
    public override NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/add-team-member@v1",
            Author = "Jan Schutte",
            Description = "Add team members to DevOps project",

            Inputs =
            {
                ["connection"] = new NoxActionInput {
                    Id = "connection",
                    Description = "The connection established with action 'azdevops/connect@v1'",
                    Default = null!,
                    IsRequired = true
                },
                ["projectName"] = new NoxActionInput { 
                    Id = "projectName", 
                    Description = "The DevOps project name",
                    Default = "",
                    IsRequired = true
                },
                ["members"] = new NoxActionInput { 
                    Id = "members", 
                    Description = "The developers to add to the projects",
                    Default = null!,
                    IsRequired = true
                },
            }
        };
    }

    private GraphHttpClient? _graphClient;
    private string? _projectName;
    

    public override async Task BeginAsync(NoxWorkflowExecutionContext ctx, IDictionary<string,object> inputs)
    {
        var connection = (VssConnection)inputs["connection"];
        _projectName = (string)inputs["projectName"]; 
        _repoName = (string)inputs["repositoryName"];
        _repoClient = await connection.GetClientAsync<GitHttpClient>();
        _graphClient = await connection.GetClientAsync<GraphHttpClient>();
    }

    public override async Task<IDictionary<string, object>> ProcessAsync(NoxWorkflowExecutionContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        _state = ActionState.Error;

        if (_repoClient == null || string.IsNullOrEmpty(_repoName) || string.IsNullOrEmpty(_projectName))
        {
            _errorMessage = "The devops create-repo action was not initialized";
        }
        else
        {
            try
            {
                var repo = await _repoClient.GetRepositoryAsync(_projectName, _repoName);
                outputs["repository"] = repo;
                _state = ActionState.Success;
            }
            catch
            {
                try
                {
                    //Create the Repo
                    var repo = await CreateRepositoryAsync();
                    if (repo != null)
                    {
                        outputs["repository"] = repo;
                        _state = ActionState.Success;
                    }
                }
                catch(Exception ex)
                {
                    _errorMessage = ex.Message;
                }
            }
        }

        return outputs;
    }

    public override Task EndAsync(NoxWorkflowExecutionContext ctx)
    {
        _repoClient?.Dispose();
        _graphClient.Dispose();
        return Task.CompletedTask;
    }
    
    private async Task<GitRepository?> CreateRepositoryAsync()
    {
        var repoCreateParameters = new GitRepository()
        {
            Name = _repoName,
        };

        GitRepository repo = null!;
        try
        {
            repo = await _repoClient.CreateRepositoryAsync(repoCreateParameters, _projectName);
            return repo;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }

        return null;
    }