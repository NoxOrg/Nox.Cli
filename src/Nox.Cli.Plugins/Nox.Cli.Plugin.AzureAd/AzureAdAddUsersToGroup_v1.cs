using Microsoft.Graph;
using Microsoft.Graph.Models;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Core.Configuration;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugins.AzDevops;

public class AzureAdAddUsersToGroup_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azuread/add-users-to-group@v1",
            Author = "Jan Schutte",
            Description = "Add a list of users to an Azure Active Directory group",

            Inputs =
            {
                ["aad-client"] = new NoxActionInput
                {
                    Id = "aad-client",
                    Description = "The AAD client",
                    Default = new GraphServiceClient(new HttpClient()),
                    IsRequired = true
                },
                
                ["group-id"] = new NoxActionInput
                {
                    Id = "group-id",
                    Description = "The Id of the aad group to which to add the team members",
                    Default = string.Empty,
                    IsRequired = true
                },

                ["user-object-ids"] = new NoxActionInput
                {
                    Id = "user-object-ids",
                    Description = "a Comma separated string of AAD user Object Ids to add to the group",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["is-owner"] = new NoxActionInput
                {
                    Id = "is-owner",
                    Description = "Indicator set if the users being added are to be owners of the group",
                    Default = false,
                    IsRequired = false
                } 
            },

        };
    }

    private string? _groupId;
    private GraphServiceClient? _aadClient;
    private string? _userObjectIds;
    private bool? _isOwner;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _groupId = inputs.Value<string>("group-id");
        _aadClient = inputs.Value<GraphServiceClient>("aad-client");
        _userObjectIds = inputs.Value<string>("user-object-ids");
        _isOwner = inputs.ValueOrDefault<bool>("is-owner", this);
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_aadClient == null || 
            string.IsNullOrEmpty(_groupId) || 
            string.IsNullOrEmpty(_userObjectIds) ||
            _isOwner == null)
        {
            ctx.SetErrorMessage("The az active directory add-users-to-group action was not initialized");
        }
        else
        {
            try
            {
                var objectIds = _userObjectIds.Split(',');
                var members = new List<string>();
                foreach (var objectId in objectIds)
                {
                    members.Add($"https://graph.microsoft.com/v1.0/directoryObjects/{objectId}");
                }

                var request = new Group
                {
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "members@odata.bind", members }
                    }
                };
                await _aadClient.Groups[_groupId].PatchAsync(request);

                if (_isOwner == true)
                {
                    foreach (var objectId in objectIds)
                    {
                        var ownerRequest = new ReferenceCreate
                        {
                            OdataId = $"https://graph.microsoft.com/v1.0/users/{objectId}"
                        };
                        await _aadClient.Groups[_groupId].Owners.Ref.PostAsync(ownerRequest);
                    }
                }
                
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
        return Task.CompletedTask;
    }
}