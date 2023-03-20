using Microsoft.Graph;
using Microsoft.Graph.Models;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugins.AzDevops;

public class AzureAdAddGroupToGroup_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azuread/add-group-to-group@v1",
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
                
                ["parent-group-id"] = new NoxActionInput
                {
                    Id = "parent-group-id",
                    Description = "The Id of the group into which to add the child-group as a member",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["child-group-id"] = new NoxActionInput
                {
                    Id = "child-group-id",
                    Description = "The Id of the group to add as a member of the parent-group",
                    Default = string.Empty,
                    IsRequired = true
                },
               
            }
        };
    }

    private string? _parentGroupId;
    private string? _childGroupId;
    private GraphServiceClient? _aadClient;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _aadClient = inputs.Value<GraphServiceClient>("aad-client");
        _parentGroupId = inputs.Value<string>("parent-group-id");
        _childGroupId = inputs.Value<string>("child-group-id");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_aadClient == null || 
            string.IsNullOrEmpty(_childGroupId) || 
            string.IsNullOrEmpty(_parentGroupId))
        {
            ctx.SetErrorMessage("The az active directory add-group-to-group action was not initialized");
        }
        else
        {
            try
            {
                var parentGroup = await _aadClient.Groups[_parentGroupId].GetAsync();
                if (parentGroup == null)
                {
                    ctx.SetErrorMessage("Parent group does not exist in Azure AD.");
                    return outputs;
                }
                
                var childGroup = await _aadClient.Groups[_childGroupId].GetAsync();
                if (childGroup == null)
                {
                    ctx.SetErrorMessage("Child group does not exist in Azure AD.");
                    return outputs;
                }

                var request = new ReferenceCreate
                {
                    OdataId = $"https://graph.microsoft.com/v1.0/directoryObjects/{_childGroupId}",
                };

                await _aadClient.Groups[_parentGroupId].Members.Ref.PostAsync(request);
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