using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugins.AzDevops;

public class AzureAdAddUserToGroup_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azuread/add-user-to-group@v1",
            Author = "Jan Schutte",
            Description = "Add a user to an Azure Active Directory group",

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

                ["user-object-id"] = new NoxActionInput
                {
                    Id = "user-object-id",
                    Description = "The object Id of the user to add to the group",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["is-owner"] = new NoxActionInput
                {
                    Id = "is-owner",
                    Description = "Indicator set if the user being added is to be an owner of the group",
                    Default = false,
                    IsRequired = false
                } 
            },

        };
    }

    private string? _groupId;
    private GraphServiceClient? _aadClient;
    private string? _userObjectId;
    private bool? _isOwner;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _groupId = inputs.Value<string>("group-id");
        _aadClient = inputs.Value<GraphServiceClient>("aad-client");
        _userObjectId = inputs.Value<string>("user-object-id");
        _isOwner = inputs.ValueOrDefault<bool>("is-owner", this);
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_aadClient == null || 
            string.IsNullOrEmpty(_groupId) || 
            string.IsNullOrEmpty(_userObjectId) ||
            _isOwner == null)
        {
            ctx.SetErrorMessage("The az active directory add-user-to-group action was not initialized");
        }
        else
        {
            try
            {
                var existingMembers = await _aadClient.Groups[_groupId].Members.GetAsync();
                var members = new List<string>();
                if (existingMembers!.Value!.All(em => em.Id != _userObjectId))
                {
                    members.Add($"https://graph.microsoft.com/v1.0/directoryObjects/{_userObjectId}");    
                }

                if (members.Count > 0)
                {
                    var request = new Group
                    {
                        AdditionalData = new Dictionary<string, object>
                        {
                            { "members@odata.bind", members }
                        }
                    };
                    await _aadClient.Groups[_groupId].PatchAsync(request);    
                }
                

                if (_isOwner == true)
                {
                    var existingOwners = await _aadClient.Groups[_groupId].Owners.GetAsync();
                    if (existingOwners!.Value!.All(eo => eo.Id != _userObjectId))
                    {
                        var ownerRequest = new ReferenceCreate
                        {
                            OdataId = $"https://graph.microsoft.com/v1.0/users/{_userObjectId}"
                        };
                        await _aadClient.Groups[_groupId].Owners.Ref.PostAsync(ownerRequest);    
                    }
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
}