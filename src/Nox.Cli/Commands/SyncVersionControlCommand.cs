namespace Nox.Cli.Commands;

using System.IO.Abstractions;
using Helpers;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.Operations;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

public class SyncVersionControlCommand : NoxCliCommand<SyncVersionControlCommand.Settings>
{
    public SyncVersionControlCommand(IAnsiConsole console, IConsoleWriter consoleWriter, 
        INoxConfiguration noxConfiguration, IConfiguration configuration) 
        : base(console, consoleWriter, noxConfiguration, configuration) {}

    public class Settings : CommandSettings
    {
        [CommandOption("--path")]
        public string DesignFolderPath { get; set; } = null!;
    }

    public async override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        await base.ExecuteAsync(context, settings);

        await SyncProjectAndTeamsOnAzureDevopsAsync();

        return 0;
    }

    private async Task SyncProjectAndTeamsOnAzureDevopsAsync()
    {
        _console.WriteLine();

        _consoleWriter.WriteHelpText("Sync: Azure Devops...");

        var devOpsServer = _noxConfiguration.VersionControl!.Server;

        var devOpsPat = _configuration["AZURE_DEVOPS_PAT"] ?? _configuration["AZURE-DEVOPS-PAT"];

        var connection = new VssConnection(new Uri(devOpsServer), new VssBasicCredential(string.Empty, devOpsPat));

        var projectClient = await connection.GetClientAsync<ProjectHttpClient>();

        var projectName = _noxConfiguration.VersionControl!.Project;

        _console.WriteLine();
        _consoleWriter.WriteInfo($"Azure Devops Project:");

        TeamProject project;
        try
        {
            project = await projectClient.GetProject(projectName);

            _console.WriteLine($"{projectName} (ID: {project.Id})");
        }
        catch
        {
            _consoleWriter.WriteWarning($"Project {projectName} not found on {devOpsServer}.");

            project = await CreateProjectAsync(connection, projectName);

            _console.WriteLine($" ...created Project {projectName} (ID: {project.Id})");
        }

        _console.WriteLine();
        _consoleWriter.WriteInfo($"Azure Devops Repository:");

        var repoClient = await connection.GetClientAsync<GitHttpClient>();

        var repoName = _noxConfiguration.VersionControl!.Repository;

        GitRepository repo;

        try
        {
            repo = await repoClient.GetRepositoryAsync(projectName, repoName);

            _console.WriteLine($"{repoName} (ID: {repo.Id})");
        }
        catch
        {
            _consoleWriter.WriteWarning($"Repository {repoName} for project {projectName} not found on {devOpsServer}.");

            repo = await CreateRepositoryAsync(connection, projectName, repoName);

            _console.WriteLine($" ...created Repository {repoName} (ID: {repo.Id})");
        }


        _console.WriteLine();
        _consoleWriter.WriteInfo($"Azure Devops Team:");
        
        var projectTeamRef = project.DefaultTeam;

        var teamClient = await connection.GetClientAsync<TeamHttpClient>();

        var graphClient = await connection.GetClientAsync<GraphHttpClient>();

        List<GraphGroup> graphGroups = new();
        
        var groupsInGraph = await graphClient.ListGroupsAsync();

        while (groupsInGraph.ContinuationToken is not null)
        {
            foreach (var group in groupsInGraph.GraphGroups)
            {
                if (group.PrincipalName.StartsWith($"[{projectName}]", StringComparison.OrdinalIgnoreCase))
                {
                    graphGroups.Add(group);
                }
            }
            groupsInGraph = await graphClient.ListGroupsAsync(continuationToken: groupsInGraph.ContinuationToken.FirstOrDefault());
        }

        var graphGroup = graphGroups.FirstOrDefault( g => g.PrincipalName.Contains($"\\{projectName} Team", StringComparison.OrdinalIgnoreCase));

        var graphAdminGroup = graphGroups.FirstOrDefault(g => g.PrincipalName.Contains($"\\Project Administrators", StringComparison.OrdinalIgnoreCase));

        var usersInGraph = graphClient.ListUsersAsync(new string[] {"aad"}).Result;
        while (usersInGraph.ContinuationToken is not null)
        {
            foreach (var user in usersInGraph.GraphUsers.OrderBy(u => u.DisplayName))
            {

                var developer = _noxConfiguration.Team!.Developers.FirstOrDefault(d => d.UserName.Equals(user.PrincipalName, StringComparison.OrdinalIgnoreCase));

                if (developer != null)
                {
                    _console.WriteLine($"{user.DisplayName} [{user.PrincipalName}]");

                    var isUserInGroup = await graphClient.CheckMembershipExistenceAsync(user.Descriptor, graphGroup!.Descriptor);
                    if (isUserInGroup)
                    {
                        _console.WriteLine($" ...is already a member of DevOps Group {graphGroup.PrincipalName}");
                    }
                    else
                    {
                        var membership = await graphClient.AddMembershipAsync(user.Descriptor, graphGroup!.Descriptor);
                        _console.WriteLine($" ...added as member of DevOps Group {graphGroup.PrincipalName}");
                    }

                    var isUserInAdminGroup = await graphClient.CheckMembershipExistenceAsync(user.Descriptor, graphAdminGroup!.Descriptor);
                    if (developer.IsAdmin)
                    {
                        if (!isUserInAdminGroup)
                        {
                            var membership = await graphClient.AddMembershipAsync(user.Descriptor, graphAdminGroup!.Descriptor);
                            _console.WriteLine($" ...added as member of DevOps Group {graphAdminGroup.PrincipalName}");
                        }
                    }
                    else
                    {
                        if (isUserInAdminGroup)
                        {
                            await graphClient.RemoveMembershipAsync(user.Descriptor, graphAdminGroup!.Descriptor);
                            _console.WriteLine($" ...removed as member of DevOps Group {graphAdminGroup.PrincipalName}");
                        }
                    }

                }
            }
            usersInGraph = await graphClient.ListUsersAsync(new string[] {"aad"}, continuationToken: usersInGraph.ContinuationToken.FirstOrDefault());
        }
    }

    private async Task<TeamProject> CreateProjectAsync(VssConnection connection, string projectName)
    {
        string projectDescription = _noxConfiguration.Description;

        string processName = "Agile";

        // Setup version control properties

        var versionControlProperties = new Dictionary<string, string>
        {
            [TeamProjectCapabilitiesConstants.VersionControlCapabilityAttributeName] =
            SourceControlTypes.Git.ToString()
        };

        // Setup process properties

        var processClient = await connection.GetClientAsync<ProcessHttpClient>();

        var processes = await processClient.GetProcessesAsync();

        Guid processId = processes.Find(process => { 
            return process.Name.Equals(processName, StringComparison.InvariantCultureIgnoreCase); 
        })!.Id;

        var processProperties = new Dictionary<string, string>
        {
            [TeamProjectCapabilitiesConstants.ProcessTemplateCapabilityTemplateTypeIdAttributeName] =
            processId.ToString()
        };

        // Construct capabilities dictionary

        var capabilities = new Dictionary<string, Dictionary<string, string>>
        {
            [TeamProjectCapabilitiesConstants.VersionControlCapabilityName] =
            versionControlProperties,

            [TeamProjectCapabilitiesConstants.ProcessTemplateCapabilityName] =
            processProperties
        };

        // Construct object containing properties needed for creating the project

        var projectCreateParameters = new TeamProject()
        {
            Name = projectName,
            Description = projectDescription,
            Capabilities = capabilities,
            Visibility = ProjectVisibility.Private
        };

        // Get a client
        var projectClient = await connection.GetClientAsync<ProjectHttpClient>();

        TeamProject project = null!;
        try
        {
            _console.WriteLine("Queuing project creation...");

            // Queue the project creation operation 
            // This returns an operation object that can be used to check the status of the creation
            var operation = await projectClient.QueueCreateProject(projectCreateParameters);

            // Check the operation status every 5 seconds (for up to 30 seconds)
            var completedOperation = await WaitForLongRunningOperation(connection, operation.Id, 5, 30);

            // Check if the operation succeeded (the project was created) or failed
            if (completedOperation.Status == Microsoft.VisualStudio.Services.Operations.OperationStatus.Succeeded)
            {
                // Get the full details about the newly created project
                project = await projectClient.GetProject(
                    projectCreateParameters.Name,
                    includeCapabilities: true,
                    includeHistory: true);
            }
            else
            {
                _consoleWriter.WriteError($"Project creation operation failed: {completedOperation.ResultMessage}");
            }
        }
        catch (Exception ex)
        {
            _consoleWriter.WriteError($"Exception during create project: {ex.Message}");
        }

        return project;
    }

    private async Task<Microsoft.VisualStudio.Services.Operations.Operation> WaitForLongRunningOperation(VssConnection connection, Guid operationId, int interavalInSec = 5, int maxTimeInSeconds = 60, CancellationToken cancellationToken = default(CancellationToken))
    {
        OperationsHttpClient operationsClient = await connection.GetClientAsync<OperationsHttpClient>();
        DateTime expiration = DateTime.Now.AddSeconds(maxTimeInSeconds);
        int checkCount = 0;

        while (true)
        {
            _console.WriteLine($" Checking status ({checkCount++})... ");

            var operation = await operationsClient.GetOperation(operationId, cancellationToken: cancellationToken);

            if (!operation.Completed)
            {
                _console.WriteLine($"   Pausing {interavalInSec} seconds");

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

    private async Task<GitRepository> CreateRepositoryAsync(VssConnection connection, string projectName, string repoName)
    {
        var repoClient = await connection.GetClientAsync<GitHttpClient>();

        var repoCreateParameters = new GitRepository()
        {
            Name = repoName,
        };

        GitRepository repo = null!;
        try
        {
            repo = await repoClient.CreateRepositoryAsync(repoCreateParameters, projectName);
        }
        catch (Exception ex)
        {
            _consoleWriter.WriteError($"Exception during create repository: {ex.Message}");
        }
        return repo;
    }
}