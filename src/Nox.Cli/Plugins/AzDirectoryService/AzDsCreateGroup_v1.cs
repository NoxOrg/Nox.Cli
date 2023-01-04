using Azure.Identity;
using Microsoft.Graph;
using Nox.Cli.Actions;
using ActionState = Nox.Cli.Actions.ActionState;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDsCreateGroup_v1 : NoxAction
{
    public override NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azds/create-group@v1",
            Author = "Jan Schutte",
            Description = "Create an Azure Active Directory group",

            Inputs =
            {
                ["aad-client"] = new NoxActionInput
                {
                    Id = "aad-client",
                    Description = "The AAD client",
                    Default = new GraphServiceClient("", null),
                    IsRequired = true
                },
                
                ["group-name"] = new NoxActionInput
                {
                    Id = "group-name",
                    Description = "The name of the aad group to create",
                    Default = string.Empty,
                    IsRequired = true
                },

                ["group-description"] = new NoxActionInput
                {
                    Id = "group-description",
                    Description = "The description of the group to create",
                    Default = string.Empty,
                    IsRequired = false
                },
                
                ["project-name"] = new NoxActionInput
                {
                    Id = "project-name",
                    Description = "The name of your project",
                    Default = string.Empty,
                    IsRequired = false
                },
            },

            Outputs =
            {
                ["aad-group"] = new NoxActionOutput
                {
                    Id = "aadGroup",
                    Description = "The AAD group that was created",
                },
            }
        };
    }

    private string? _groupName;
    private string? _groupDescription;
    private string? _projectName;
    private GraphServiceClient? _aadClient;

    public override Task BeginAsync(NoxWorkflowExecutionContext ctx, IDictionary<string, object> inputs)
    {
        _groupName = (string)inputs["group-name"];
        _groupDescription = (string)inputs["group-description"];
        _aadClient = (GraphServiceClient)inputs["aad-client"];
        _projectName = inputs.ContainsKey("project-name") ? (string)inputs["project-name"] : "";
        return Task.CompletedTask;
    }

    public override async Task<IDictionary<string, object>> ProcessAsync(NoxWorkflowExecutionContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        _state = ActionState.Error;

        if (_aadClient == null || string.IsNullOrEmpty(_groupName))
        {
            _errorMessage = "The az active directory create-group action was not initialized";
        }
        else
        {
            if (string.IsNullOrEmpty(_groupDescription)) _groupDescription = _groupName;
            try
            {
                var projectGroupName = _groupName.ToUpper();

                var projectGroups = await _aadClient.Groups.Request()
                    .Filter($"DisplayName eq '{projectGroupName}'")
                    .Expand("Members")
                    .GetAsync();

                if (projectGroups.Count == 1)
                {
                    outputs["aad-group"] = projectGroups.First();
                }
                else
                {
                    outputs["aad-group"] = await CreateAdGroupAsync(projectGroupName);
                }
                _state = ActionState.Success;
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
            }
        }

        return outputs;
    }

    public override Task EndAsync(NoxWorkflowExecutionContext ctx)
    {
        return Task.CompletedTask;
    }

    private async Task<Group> CreateAdGroupAsync(string projectGroupName)
    {
        var description = "Created by Nox.Cli";
        if (!string.IsNullOrEmpty(_projectName)) description += $" for service {_projectName}";
        var newGroup = new Group()
        {
            DisplayName = projectGroupName,
            Description = description,
            Visibility = "Private",
            SecurityEnabled = true,
            MailNickname = projectGroupName,
            MailEnabled = false,
            Owners = { },
            Members = { },
            Team = { }
        };

        var group = await _aadClient!.Groups.Request().AddAsync(newGroup);

        return group;
    }
    
}