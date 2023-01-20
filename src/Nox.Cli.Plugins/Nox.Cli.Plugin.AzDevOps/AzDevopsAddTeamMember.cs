using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.Actions;
using Nox.Core.Configuration;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsAddTeamMember_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/add-team-member@v1",
            Author = "Jan Schutte",
            Description = "Add a team member to a DevOps project",

            Inputs =
            {
                ["connection"] = new NoxActionInput
                {
                    Id = "connection",
                    Description = "The connection established with action 'azdevops/connect@v1'",
                    Default = new VssConnection(new Uri("https://localhost"), null),
                    IsRequired = true
                },
                ["project-name"] = new NoxActionInput
                {
                    Id = "project-name",
                    Description = "The DevOps project name",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["user-name"] = new NoxActionInput
                {
                    Id = "user-name",
                    Description = "The Azure Active Directory username of the team member to add to the project",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["group-name"] = new NoxActionInput
                {
                    Id = "group-name",
                    Description = "The group to add this team member to",
                    Default = "",
                    IsRequired = false
                },
            }
        };
    }
    
    private GraphHttpClient? _graphClient;
    private string? _projectName;
    private string? _username;
    private string? _groupName;

    public async Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string, object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectName = inputs.Value<string>("project-name");
        _username = inputs.Value<string>("user-name");
        _groupName = inputs.Value<string>("group-name");
        _graphClient = await connection!.GetClientAsync<GraphHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_graphClient == null || string.IsNullOrEmpty(_projectName) || _username == null)
        {
            ctx.SetErrorMessage("The devops add-team-member action was not initialized");
        }
        else
        {
            try
            {
                await AddTeamMember();
                ctx.SetState(ActionState.Success);
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
        _graphClient?.Dispose();
        return Task.CompletedTask;
    }

    private async Task AddTeamMember()
    {
        List<GraphGroup> graphGroups = new();
        
        var groupsInGraph = await _graphClient!.ListGroupsAsync();
    
        while (groupsInGraph.ContinuationToken is not null)
        {
            foreach (var group in groupsInGraph.GraphGroups)
            {
                if (group.PrincipalName.StartsWith($"[{_projectName}]", StringComparison.OrdinalIgnoreCase))
                {
                    graphGroups.Add(group);
                }
            }
            groupsInGraph = await _graphClient.ListGroupsAsync(continuationToken: groupsInGraph.ContinuationToken.FirstOrDefault());
        }
    
        var graphTeamGroup = graphGroups.FirstOrDefault( g => g.PrincipalName.Contains($"\\{_projectName} Team", StringComparison.OrdinalIgnoreCase));

        GraphGroup? graphSpecificGroup = null;
        if (!string.IsNullOrEmpty(_groupName))
        {
            graphSpecificGroup = graphGroups.FirstOrDefault(g => g.PrincipalName.Contains($"\\{_groupName}", StringComparison.OrdinalIgnoreCase));    
        }
    
        var usersInGraph = _graphClient.ListUsersAsync(new string[] {"aad"}).Result;
        while (usersInGraph.ContinuationToken is not null)
        {
            var user = usersInGraph.GraphUsers.FirstOrDefault(u => u.PrincipalName.Equals(_username, StringComparison.OrdinalIgnoreCase));
            if (user != null)
            {
                var isUserInGroup = await _graphClient.CheckMembershipExistenceAsync(user.Descriptor, graphTeamGroup!.Descriptor);
                if (!isUserInGroup)
                {
                    var membership = await _graphClient.AddMembershipAsync(user.Descriptor, graphTeamGroup!.Descriptor);
                }

                if (graphSpecificGroup != null)
                {
                    var isUserInSpecificGroup = await _graphClient.CheckMembershipExistenceAsync(user.Descriptor, graphSpecificGroup!.Descriptor);
                    if (!isUserInSpecificGroup)
                    {
                        var membership = await _graphClient.AddMembershipAsync(user.Descriptor, graphSpecificGroup!.Descriptor);
                    }
                }
            }
            usersInGraph = await _graphClient.ListUsersAsync(new string[] {"aad"}, continuationToken: usersInGraph.ContinuationToken.FirstOrDefault());
        }
    }
}