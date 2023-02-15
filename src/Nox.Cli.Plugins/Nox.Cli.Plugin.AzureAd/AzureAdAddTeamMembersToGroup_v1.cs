using Microsoft.Graph;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Core.Configuration;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugins.AzDevops;

public class AzureAdAddTeamMembersToGroup_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azuread/add-team-members-to-group@v1",
            Author = "Jan Schutte",
            Description = "Add project team members to an Azure Active Directory group",

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

                
                ["team-members"] = new NoxActionInput
                {
                    Id = "team-members",
                    Description = "The developers to add to the project",
                    Default = new List<TeamMemberConfiguration>(),
                    IsRequired = true
                },
            },

        };
    }

    private Group? _group;
    private GraphServiceClient? _aadClient;
    private List<TeamMemberConfiguration>? _members;

    public Task BeginAsync(IDictionary<string, IVariable> inputs)
    {
        _group = inputs.Value<Group>("group");
        _aadClient = inputs.Value<GraphServiceClient>("aad-client");
        _members = inputs.Value<List<TeamMemberConfiguration>>("team-members");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, IVariable>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, IVariable>();

        ctx.SetState(ActionState.Error);

        if (_aadClient == null || _group == null || _members == null)
        {
            ctx.SetErrorMessage("The az active directory add-team-members action was not initialized");
        }
        else
        {
            try
            {
                foreach (var developer in _members)
                {
                    var users = await _aadClient.Users.Request()
                        .Filter($"UserPrincipalName eq '{developer.UserName}'")
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
                        ctx.SetErrorMessage($"AAD User {developer.UserName} not found.");
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