namespace Nox.Cli.Commands;

using System.IO.Abstractions;
using Azure.Identity;
using Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

public class SyncDirectoryServiceCommand : NoxCliCommand<SyncDirectoryServiceCommand.Settings>
{
    private const string _projectNamePrefix = "NOX_PROJECT_";

    private const string _securityGroupName = "NOX_PROJECTS_ALL";

    public SyncDirectoryServiceCommand(IAnsiConsole console, IConsoleWriter consoleWriter,
        INoxConfiguration noxConfiguration, IConfiguration configuration) 
        : base(console, consoleWriter, noxConfiguration, configuration) { }

    public class Settings : CommandSettings
    {
        [CommandOption("-p|--path")]
        public string DesignFolderPath { get; set; } = null!;
    }

    public async override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        await base.ExecuteAsync(context, settings);

        await SyncGroupsAndUsersInAzureActiveDirectoryAsync();

        return 0;
    }

    private async Task SyncGroupsAndUsersInAzureActiveDirectoryAsync()
    {
        _console.WriteLine();

        _consoleWriter.WriteHelpText("Sync: Azure AD...");

        var tenantId = _configuration["AZURE_TENANT_ID"];
        var clientId = _configuration["AZURE_CLIENT_ID"];
        var clientSecret = _configuration["AZURE_CLIENT_SECRET"];

        var _userScopes = new string[] { @"https://graph.microsoft.com/.default" };

        var _credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        var _userClient = new GraphServiceClient(_credential, _userScopes);

        var projectGroupName = $"{_projectNamePrefix}{_noxConfiguration.Name.ToUpper()}";

        var projectGroups = await _userClient.Groups.Request()
            .Filter($"DisplayName eq '{projectGroupName}'")
            .Expand("Members")
            .GetAsync();

        Group? projectGroup;

        _console.WriteLine();
        _consoleWriter.WriteInfo($"AAD Group:");

        if (projectGroups.Count == 1)
        {
            projectGroup = projectGroups.First();

            _console.WriteLine($"{projectGroup.DisplayName} (Id:{projectGroup.Id}) [{projectGroup.Members?.Count ?? 0} members]");
        }
        else
        {
            _consoleWriter.WriteWarning($"Group {projectGroupName} not found");

            projectGroup = await CreateAdGroupAsync(_userClient, projectGroupName);

            _console.WriteLine($" ...created AAD Group: {projectGroupName} [{projectGroup.Id}]");
        }

        _console.WriteLine();
        _consoleWriter.WriteInfo($"AAD Security Group:");

        var securityGroups = await _userClient.Groups.Request()
            .Filter($"DisplayName eq '{_securityGroupName}'")
            .Expand("Members")
            .GetAsync();

        Group? securityGroup;

        if (securityGroups.Count == 1)
        {
            securityGroup = securityGroups.First();
            _console.WriteLine($"{securityGroup.DisplayName} (Id:{securityGroup.Id}) [{securityGroup.Members?.Count ?? 0} members]");
        }
        else
        {
            _consoleWriter.WriteWarning($"Group {_securityGroupName} not found");

            securityGroup = await CreateAdGroupAsync(_userClient, $"{_securityGroupName}");

            _console.WriteLine($" ...created AAD Group: {_securityGroupName} [{securityGroup.Id}]");
        }

        if (securityGroup.Members is null || securityGroup.Members.FirstOrDefault(u => u.Id.Equals(projectGroup.Id)) is null)
        {
            await _userClient.Groups[securityGroup.Id].Members.References.Request().AddAsync(projectGroup);
            _console.WriteLine($" ...added group {projectGroupName} as a member");
        }
        else
        {
            _console.WriteLine($" ...already has group {projectGroupName} as a member");
        }


        _console.WriteLine();
        _consoleWriter.WriteInfo($"AAD Users:");

        foreach (var developer in _noxConfiguration.Team!.Developers)
        {
            var users = await _userClient.Users.Request()
                .Filter($"UserPrincipalName eq '{developer.UserName}'")
                .GetAsync();

            if (users.Count == 1)
            {
                var user = users.First();
                _console.WriteLine($"{user.DisplayName} [{user.UserPrincipalName}] (Id:{user.Id}");

                if (projectGroup.Members is null || projectGroup.Members.FirstOrDefault(u => u.Id.Equals(user.Id)) is null)
                {
                    await _userClient.Groups[projectGroup.Id].Members.References.Request().AddAsync(user);
                    _console.WriteLine($" ...was added to AAD Group {projectGroupName}");
                }
                else
                {
                    _console.WriteLine($" ...is already a member of AAD Group {projectGroupName}");
                }
            }
            else
            {
                _consoleWriter.WriteError($"AAD User {developer.UserName} not found.");
            }
        }
    }

    private async Task<Group> CreateAdGroupAsync(GraphServiceClient _userClient, string projectGroupName)
    {
        var newGroup = new Group()
        {
            DisplayName = projectGroupName,
            Description = $"Created by Nox.Cli for service [{_noxConfiguration.Name}]. {_noxConfiguration.Description}",
            Visibility = "Private",
            SecurityEnabled = true,
            MailNickname = projectGroupName,
            MailEnabled = false,
            Owners = { },
            Members = { },
            Team = { }
        };

        var group = await _userClient.Groups.Request().AddAsync(newGroup);

        return group;
    }

}