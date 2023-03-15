using Microsoft.Graph;
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
                
                ["group"] = new NoxActionInput
                {
                    Id = "group",
                    Description = "The aad group to which to add the team members",
                    Default = new Group(),
                    IsRequired = true
                },

                
                ["user-names"] = new NoxActionInput
                {
                    Id = "user-names",
                    Description = "The comma separated string of AAD user names to add to the group",
                    Default = string.Empty,
                    IsRequired = true
                },
            },

        };
    }

    private Group? _group;
    private GraphServiceClient? _aadClient;
    private string? _userNames;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _group = inputs.Value<Group>("group");
        _aadClient = inputs.Value<GraphServiceClient>("aad-client");
        _userNames = inputs.Value<string>("user-names");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_aadClient == null || 
            _group == null || 
            string.IsNullOrEmpty(_userNames))
        {
            ctx.SetErrorMessage("The az active directory add-users-to-group action was not initialized");
        }
        else
        {
            try
            {
                var userNames = _userNames.Split(',');
                foreach (var userName in userNames)
                {
                    var users = await _aadClient.Users.Request()
                        .Filter($"UserPrincipalName eq '{userName}'")
                        .GetAsync();
                    
                    if (users.Count == 1)
                    {
                        var user = users.First();
                        if (_group.Members is null || _group.Members.FirstOrDefault(u => u.Id.Equals(user.Id)) is null)
                        {
                            await _aadClient.Groups[_group.Id].Members.References.Request().AddAsync(user);
                        }
                    }
                    else
                    {
                        ctx.SetErrorMessage($"AAD User {userName} not found.");
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