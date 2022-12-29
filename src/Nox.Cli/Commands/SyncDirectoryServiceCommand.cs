namespace Nox.Cli.Commands;

using System.IO.Abstractions;
using Azure.Identity;
using Helpers;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

public class SyncDirectoryServiceCommand : NoxCliCommand<SyncDirectoryServiceCommand.Settings>
{

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
        // var armClient = new ArmClient(new DefaultAzureCredential());
        // SubscriptionResource subscription = armClient.GetDefaultSubscription();

        _console.WriteLine();

        _consoleWriter.WriteHelpText("Sync: Azure AD...");

        var tenantId = _configuration["AZURE_TENANT_ID"];
        var clientId = _configuration["AZURE_CLIENT_ID"];
        var clientSecret = _configuration["AZURE_CLIENT_SECRET"];

        var _userScopes = new string[] { @"https://graph.microsoft.com/.default" };

        var _credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        var _userClient = new GraphServiceClient(_credential, _userScopes);

        var projectGroupName = $"NOX_PROJECT_{_noxConfiguration.Name.ToUpper()}";

        var groups = await _userClient.Groups.Request()
            .Filter($"DisplayName eq '{projectGroupName}'")
            .Expand("Members")
            .GetAsync();

        Group? group;

        _console.WriteLine();
        _consoleWriter.WriteInfo($"AAD Group:");

        if (groups.Count == 1)
        {
            group = groups.First();
            _console.WriteLine($"{group.DisplayName} (Id:{group.Id}) [{group.Members?.Count ?? 0} members]");
        }
        else
        {
            _consoleWriter.WriteWarning($"Group {projectGroupName} not found");

            group = await CreateAdGroupAsync(_userClient, projectGroupName);

            _console.WriteLine($" ...created AAD Group: {projectGroupName} [{group.Id}]");
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

                if (group.Members is null || group.Members.FirstOrDefault(u => u.Id.Equals(user.Id)) is null)
                {
                    await _userClient.Groups[group.Id].Members.References.Request().AddAsync(user);
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