using AutoMapper;
using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Actions;
using Nox.Core.Configuration;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsAddTeamMembers_v1 : NoxAction
{
    public override NoxActionMetaData Discover()
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
                    Default = null!,
                    IsRequired = true
                },
                ["projectName"] = new NoxActionInput
                {
                    Id = "projectName",
                    Description = "The DevOps project name",
                    Default = "",
                    IsRequired = true
                },
                ["teamMembers"] = new NoxActionInput
                {
                    Id = "teamMembers",
                    Description = "The developers to add to the project",
                    Default = null!,
                    IsRequired = true
                },
            }
        };
    }

    private GraphHttpClient? _graphClient;
    private string? _projectName;
    private List<TeamMemberConfiguration>? _members;

    public override async Task BeginAsync(NoxWorkflowExecutionContext ctx, IDictionary<string, object> inputs)
    {
        var connection = (VssConnection)inputs["connection"];
        _projectName = (string)inputs["projectName"];
        _members = GetTeamMembers((List<object>)inputs["teamMembers"]);
        _graphClient = await connection.GetClientAsync<GraphHttpClient>();
    }

    public override async Task<IDictionary<string, object>> ProcessAsync(NoxWorkflowExecutionContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        _state = ActionState.Error;

        if (_graphClient == null || string.IsNullOrEmpty(_projectName) || _members == null || _members!.Count == 0)
        {
            _errorMessage = "The devops create-repo action was not initialized";
        }
        else
        {
            try
            {
                await AddTeamMembers();
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

    private List<TeamMemberConfiguration> GetTeamMembers(List<object> source)
    {
        var result = new List<TeamMemberConfiguration>();
        foreach (Dictionary<object, object> item in source)
        {
            result.Add(new TeamMemberConfiguration
            {
                Name = item.GetValueOrDefault("name").ToString(),
                UserName = item.GetValueOrDefault("userName").ToString(),
                IsAdmin = item.GetValueOrDefault("isAdmin").ToString() == "true"
            });
        }
        return result;
    }
    
}