using Nox.Cli.Actions;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Operations;
using Microsoft.VisualStudio.Services.WebApi;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsCreateProject_v1 : NoxAction
{
    public override NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/create-Project@v1",
            Author = "Jan Schutte",
            Description = "Create an Azure Devops project",

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
                
                ["project-description"] = new NoxActionInput { 
                    Id = "project-description", 
                    Description = "The description of the DevOps project",
                    Default = string.Empty,
                    IsRequired = false
                },
            },

            Outputs =
            {
                ["project"] = new NoxActionOutput {
                    Id = "project",
                    Description = "The Azure devops project",
                },
            }
        };
    }

    private ProjectHttpClient? _projectClient;
    private ProcessHttpClient? _processClient;
    private OperationsHttpClient? _operationsClient;
    private string? _projectName;
    private string? _projectDescription;

    public override async Task BeginAsync(NoxWorkflowExecutionContext ctx, IDictionary<string,object> inputs)
    {
        var connection = (VssConnection)inputs["connection"];
        _projectName = (string)inputs["project-name"];
        _projectDescription = (string)inputs["project-description"];
        _projectClient = await connection.GetClientAsync<ProjectHttpClient>();
        _processClient = await connection.GetClientAsync<ProcessHttpClient>();
        _operationsClient = await connection.GetClientAsync<OperationsHttpClient>();
    }

    public override async Task<IDictionary<string, object>> ProcessAsync(NoxWorkflowExecutionContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        _state = ActionState.Error;

        if (_projectClient == null || string.IsNullOrEmpty(_projectName) || _processClient == null || _operationsClient == null)
        {
            _errorMessage = "The devops create-project action was not initialized";
        }
        else
        {
            if (string.IsNullOrEmpty(_projectDescription)) _projectDescription = _projectName;
            try
            {
                var project = await _projectClient.GetProject(_projectName);
                outputs["project"] = project;
                _state = ActionState.Success;
            }
            catch
            {
                try
                {
                    //Create the project
                    var project = await CreateProjectAsync();
                    if (project != null)
                    {
                        outputs["project"] = project;
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
        _projectClient?.Dispose();
        _processClient?.Dispose();
        _operationsClient?.Dispose();
        return Task.CompletedTask;
    }
    
    private async Task<TeamProject?> CreateProjectAsync()
    {
        var processName = "Agile";

        // Setup version control properties

        var versionControlProperties = new Dictionary<string, string>
        {
            [TeamProjectCapabilitiesConstants.VersionControlCapabilityAttributeName] = SourceControlTypes.Git.ToString()
        };

        // Setup process properties

        var processes = await _processClient!.GetProcessesAsync();

        var processId = processes.Find(process => process.Name.Equals(processName, StringComparison.InvariantCultureIgnoreCase))!.Id;

        var processProperties = new Dictionary<string, string>
        {
            [TeamProjectCapabilitiesConstants.ProcessTemplateCapabilityTemplateTypeIdAttributeName] = processId.ToString()
        };

        // Construct capabilities dictionary

        var capabilities = new Dictionary<string, Dictionary<string, string>>
        {
            [TeamProjectCapabilitiesConstants.VersionControlCapabilityName] = versionControlProperties,

            [TeamProjectCapabilitiesConstants.ProcessTemplateCapabilityName] = processProperties
        };

        // Construct object containing properties needed for creating the project

        var projectCreateParameters = new TeamProject()
        {
            Name = _projectName,
            Description = _projectDescription,
            Capabilities = capabilities,
            Visibility = ProjectVisibility.Private
        };

        TeamProject project = null!;
        try
        {
            // Queue the project creation operation 
            // This returns an operation object that can be used to check the status of the creation
            var operation = await _projectClient!.QueueCreateProject(projectCreateParameters);

            // Check the operation status every 5 seconds (for up to 30 seconds)
            var completedOperation = await WaitForLongRunningOperation(operation.Id, 5, 30);

            // Check if the operation succeeded (the project was created) or failed
            if (completedOperation.Status == Microsoft.VisualStudio.Services.Operations.OperationStatus.Succeeded)
            {
                // Get the full details about the newly created project
                project = await _projectClient.GetProject(
                    projectCreateParameters.Name,
                    includeCapabilities: true,
                    includeHistory: true);
                return project;
            }
            else
            {
                _errorMessage = $"Project creation operation failed: {completedOperation.ResultMessage}";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }

        return null;
    }

    private async Task<Microsoft.VisualStudio.Services.Operations.Operation> WaitForLongRunningOperation(Guid operationId, int interavalInSec = 5, int maxTimeInSeconds = 60, CancellationToken cancellationToken = default(CancellationToken))
    {
        var expiration = DateTime.Now.AddSeconds(maxTimeInSeconds);

        while (true)
        {
            var operation = await _operationsClient!.GetOperation(operationId, cancellationToken: cancellationToken);

            if (!operation.Completed)
            {
                await Task.Delay(interavalInSec * 1000, cancellationToken);

                if (DateTime.Now > expiration)
                {
                    throw new Exception(String.Format("Operation did not complete in {0} seconds.", maxTimeInSeconds));
                }
            }
            else
            {
                return operation;
            }
        }
    }
}

