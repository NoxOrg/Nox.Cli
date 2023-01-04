using Microsoft.Graph;
using Nox.Cli.Actions;
using Nox.Cli.Helpers;
using Nox.Core.Configuration;
using ActionState = Nox.Cli.Actions.ActionState;

public class AzDsAddTeamMembers_v1 : NoxAction
{
    public override NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azds/add-team-members@v1",
            Author = "Jan Schutte",
            Description = "Add project team members to an Azure Active Directory group",

            Inputs =
            {
                ["aad-client"] = new NoxActionInput
                {
                    Id = "aad-client",
                    Description = "The AAD client",
                    Default = new GraphServiceClient("", null),
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

    public override Task BeginAsync(NoxWorkflowExecutionContext ctx, IDictionary<string, object> inputs)
    {
        _group = (Group)inputs["group"];
        _aadClient = (GraphServiceClient)inputs["aad-client"];
        _members = ((List<object>)inputs["team-members"]).ToTeamMemberConfiguration();
        return Task.CompletedTask;
    }

    public override async Task<IDictionary<string, object>> ProcessAsync(NoxWorkflowExecutionContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        _state = ActionState.Error;

        if (_aadClient == null || _group == null || _members == null)
        {
            _errorMessage = "The az active directory add-team-members action was not initialized";
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
                        _errorMessage = $"AAD User {developer.UserName} not found.";
                    }
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
}