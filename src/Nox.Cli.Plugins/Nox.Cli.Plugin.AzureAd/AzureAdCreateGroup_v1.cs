using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugins.AzDevops;

public class AzureAdCreateGroup_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azuread/create-group@v1",
            Author = "Jan Schutte",
            Description = "Create an Azure Active Directory group",

            Inputs =
            {
                ["aad-client"] = new NoxActionInput
                {
                    Id = "aad-client",
                    Description = "The AAD client",
                    Default = new GraphServiceClient(new HttpClient()),
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
                ["group-id"] = new NoxActionOutput
                {
                    Id = "group-id",
                    Description = "The Id of the AAD group that was created.",
                },
            }
        };
    }

    private string? _groupName;
    private string? _groupDescription;
    private string? _projectName;
    private GraphServiceClient? _aadClient;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _groupName = inputs.Value<string>("group-name");
        _groupDescription = inputs.Value<string>("group-description");
        _aadClient = inputs.Value<GraphServiceClient>("aad-client");
        _projectName = inputs.ValueOrDefault<string>("project-name", this);
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_aadClient == null || string.IsNullOrEmpty(_groupName))
        {
            ctx.SetErrorMessage("The az active directory create-group action was not initialized");
        }
        else
        {
            if (string.IsNullOrEmpty(_groupDescription)) _groupDescription = _groupName;
            try
            {
                var projectGroupName = _groupName.ToUpper();

                var projectGroups = await _aadClient.Groups.GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Count = true;
                    requestConfiguration.QueryParameters.Filter = $"displayName eq '{projectGroupName}'";
                    requestConfiguration.QueryParameters.Select = new string []{ "id","displayName" };
                });
                
                if (projectGroups != null && projectGroups.Value!.Count == 1)
                {
                    outputs["group-id"] = projectGroups.Value.First().Id!;
                }
                else
                {
                    var newGroupId = await CreateAdGroupAsync(projectGroupName);
                    if (newGroupId != null) outputs["group-id"] = newGroupId;
                }
                ctx.SetState(ActionState.Success);
            }
            catch (ODataError odataError)
            {
                ctx.SetErrorMessage(odataError.Error!.Message!);
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
        return Task.CompletedTask;
    }

    private async Task<string?> CreateAdGroupAsync(string projectGroupName)
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

        var group = await _aadClient!.Groups.PostAsync(newGroup);
        return group != null ? group.Id! : null;
    }
    
}