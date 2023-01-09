using Microsoft.Graph;
using Nox.Cli.Actions;
using ActionState = Nox.Cli.Actions.ActionState;

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
                    Default = new GraphServiceClient("", null),
                    IsRequired = true
                },
                
                ["child-group"] = new NoxActionInput
                {
                    Id = "child-group",
                    Description = "The group to add as a member of the parent-group",
                    Default = new Group(),
                    IsRequired = true
                },

                ["parent-group"] = new NoxActionInput
                {
                    Id = "parent-group",
                    Description = "The group into which to add the child-group as a member",
                    Default = new Group(),
                    IsRequired = true
                },
            }
        };
    }

    private Group? _childGroup;
    private Group? _parentGroup;
    private GraphServiceClient? _aadClient;

    public Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string, object> inputs)
    {
        _aadClient = (GraphServiceClient)inputs["aad-client"];
        _childGroup = (Group)inputs["child-group"];
        _parentGroup = (Group)inputs["parent-group"];
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_aadClient == null || _childGroup == null || _parentGroup == null)
        {
            ctx.SetErrorMessage("The az active directory add-group-to-group action was not initialized");
        }
        else
        {
            try
            {
                Group? parentGroup;
                var parentGroups = await _aadClient.Groups.Request()
                    .Filter($"DisplayName eq '{_parentGroup.DisplayName}'")
                    .Expand("Members")
                    .GetAsync();
                
                if (parentGroups.Count == 1)
                {
                    parentGroup = parentGroups.First();
                    
                    Group? childGroup;
                    var childGroups = await _aadClient.Groups.Request()
                        .Filter($"DisplayName eq '{_childGroup.DisplayName}'")
                        .Expand("Members")
                        .GetAsync();
                    if (childGroups.Count == 1)
                    {
                        childGroup = childGroups.First();
                        if (parentGroup.Members is null || parentGroup.Members.FirstOrDefault(u => u.Id.Equals(childGroup.Id)) is null)
                        {
                            await _aadClient.Groups[parentGroup.Id].Members.References.Request().AddAsync(childGroup);
                        }
                        ctx.SetState(ActionState.Success);
                    }
                    else
                    {
                        ctx.SetErrorMessage($"The group {_childGroup} does not exist in your Azure Active Directory");
                    }
                }
                else
                {
                    ctx.SetErrorMessage($"The group {_parentGroup} does not exist in your Azure Active Directory");
                }
            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage(ex.Message);
            }
        }

        return outputs;
    }

    public Task EndAsync(INoxWorkflowContext ctx)
    {
        return Task.CompletedTask;
    }
}