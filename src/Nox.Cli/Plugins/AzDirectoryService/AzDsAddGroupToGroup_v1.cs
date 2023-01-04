using Azure.Identity;
using Microsoft.Graph;
using Nox.Cli.Actions;
using Nox.Core.Configuration;
using ActionState = Nox.Cli.Actions.ActionState;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDsAddGroupToGroup_v1 : NoxAction
{
    public override NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azds/add-group-to-group@v1",
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

    public override Task BeginAsync(NoxWorkflowExecutionContext ctx, IDictionary<string, object> inputs)
    {
        _aadClient = (GraphServiceClient)inputs["aad-client"];
        _childGroup = (Group)inputs["child-group"];
        _parentGroup = (Group)inputs["parent-group"];
        return Task.CompletedTask;
    }

    public override async Task<IDictionary<string, object>> ProcessAsync(NoxWorkflowExecutionContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        _state = ActionState.Error;

        if (_aadClient == null || _childGroup == null || _parentGroup == null)
        {
            _errorMessage = "The az active directory add-group-to-group action was not initialized";
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
                        _state = ActionState.Success;
                    }
                    else
                    {
                        _errorMessage = $"The group {_childGroup} does not exist in your Azure Active Directory";
                    }
                }
                else
                {
                    _errorMessage = $"The group {_parentGroup} does not exist in your Azure Active Directory";
                }
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
}