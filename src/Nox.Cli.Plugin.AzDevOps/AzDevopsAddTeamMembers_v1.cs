using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Actions;
using Nox.Core.Configuration;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsAddTeamMembers_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/add-team-members@v1",
            Author = "Jan Schutte",
            Description = "Add team members to DevOps project",

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
                ["team-members"] = new NoxActionInput
                {
                    Id = "team-members",
                    Description = "The developers to add to the project",
                    Default = new List<TeamMemberConfiguration>(),
                    IsRequired = true
                },
            }
        };
    }
    
    private GraphHttpClient? _graphClient;
    private string? _projectName;
    private List<TeamMemberConfiguration>? _members;

    public async Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string, object> inputs)
    {
        var connection = (VssConnection)inputs["connection"];
        _projectName = (string)inputs["project-name"];
        _members = ((List<object>)inputs["team-members"]).ToTeamMemberConfiguration();
        _graphClient = await connection.GetClientAsync<GraphHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_graphClient == null || string.IsNullOrEmpty(_projectName) || _members == null || _members!.Count == 0)
        {
            ctx.SetErrorMessage("The devops create-repo action was not initialized");
        }
        else
        {
            try
            {
                await AddTeamMembers();
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

    private async Task AddTeamMembers()
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
    
        var graphGroup = graphGroups.FirstOrDefault( g => g.PrincipalName.Contains($"\\{_projectName} Team", StringComparison.OrdinalIgnoreCase));
    
        var graphAdminGroup = graphGroups.FirstOrDefault(g => g.PrincipalName.Contains($"\\Project Administrators", StringComparison.OrdinalIgnoreCase));
    
        var usersInGraph = _graphClient.ListUsersAsync(new string[] {"aad"}).Result;
        while (usersInGraph.ContinuationToken is not null)
        {
            foreach (var user in usersInGraph.GraphUsers.OrderBy(u => u.DisplayName))
            {
                var developer = _members!.FirstOrDefault(d => d.UserName.Equals(user.PrincipalName, StringComparison.OrdinalIgnoreCase));
    
                if (developer != null)
                {
                    var isUserInGroup = await _graphClient.CheckMembershipExistenceAsync(user.Descriptor, graphGroup!.Descriptor);
                    if (!isUserInGroup)
                    {
                        var membership = await _graphClient.AddMembershipAsync(user.Descriptor, graphGroup!.Descriptor);
                    }
    
                    var isUserInAdminGroup = await _graphClient.CheckMembershipExistenceAsync(user.Descriptor, graphAdminGroup!.Descriptor);
                    if (developer.IsAdmin)
                    {
                        if (!isUserInAdminGroup)
                        {
                            var membership = await _graphClient.AddMembershipAsync(user.Descriptor, graphAdminGroup!.Descriptor);
                        }
                    }
                    else
                    {
                        if (isUserInAdminGroup)
                        {
                            await _graphClient.RemoveMembershipAsync(user.Descriptor, graphAdminGroup!.Descriptor);
                        }
                    }
    
                }
            }
            usersInGraph = await _graphClient.ListUsersAsync(new string[] {"aad"}, continuationToken: usersInGraph.ContinuationToken.FirstOrDefault());
        }
    }
}