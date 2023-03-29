using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Operations;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsEnsureProjectExists_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/ensure-project-exists@v1",
            Author = "Jan Schutte",
            Description = "Get a reference to a DevOps project, if it does not exist then create it.",

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
                ["project-id"] = new NoxActionOutput {
                    Id = "project-id",
                    Description = "The Id of the Azure devops project",
                },
                ["success-message"] = new NoxActionOutput {
                    Id = "success-message",
                    Description = "A message specifying if the project was found or created",
                },
            }
        };
    }

    private ProjectHttpClient? _projectClient;
    private ProcessHttpClient? _processClient;
    private OperationsHttpClient? _operationsClient;
    
    private string? _projectName;
    private string? _projectDescription;
    private bool _isServerContext = false;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectName = inputs.Value<string>("project-name");
        _projectDescription = inputs.Value<string>("project-description");
        if (string.IsNullOrEmpty(_projectDescription)) _projectDescription = _projectName;
        _projectClient = await connection!.GetClientAsync<ProjectHttpClient>();
        _processClient = await connection!.GetClientAsync<ProcessHttpClient>();
        _operationsClient = await connection!.GetClientAsync<OperationsHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_projectClient == null || string.IsNullOrEmpty(_projectName) || _processClient == null || _operationsClient == null)
        {
            ctx.SetErrorMessage("The devops create-project action was not initialized");
        }
        else
        {
            try
            {
                var project = await _projectClient.GetProject(_projectName);
                
                outputs["project-id"] = project.Id;
                outputs["success-message"] = $"Found existing project {_projectName} ({project.Id})";

                ctx.SetState(ActionState.Success);
            }
            catch
            {
                try
                {
                    //Create the project
                    var project = await CreateProjectAsync(ctx);
                    if (project != null)
                    {
                        outputs["project-id"] = project.Id;
                        outputs["success-message"] = $"Successfully created project {_projectName} ({project.Id})";

                        ctx.SetState(ActionState.Success);
                    }
                }
                catch(Exception ex)
                {
                    ctx.SetErrorMessage(ex.Message);
                }
            }
        }

        return outputs;
    }

    public Task EndAsync()
    {
        if (!_isServerContext)
        {
            _projectClient?.Dispose();
            _processClient?.Dispose();
            _operationsClient?.Dispose();    
        }
        return Task.CompletedTask;
    }
    
    private async Task<TeamProject?> CreateProjectAsync(INoxWorkflowContext ctx)
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
            Visibility = ProjectVisibility.Private,
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
                ctx.SetErrorMessage($"Project creation operation failed: {completedOperation.ResultMessage}");
            }
        }
        catch (Exception ex)
        {
            ctx.SetErrorMessage(ex.Message);
        }

        return null;
    }

    private async Task<Operation> WaitForLongRunningOperation(Guid operationId, int interavalInSec = 5, int maxTimeInSeconds = 60, CancellationToken cancellationToken = default(CancellationToken))
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

