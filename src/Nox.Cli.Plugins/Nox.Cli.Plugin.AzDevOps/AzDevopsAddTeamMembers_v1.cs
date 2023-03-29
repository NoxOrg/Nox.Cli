using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
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
                ["project-id"] = new NoxActionInput
                {
                    Id = "project-id",
                    Description = "The DevOps project Id",
                    Default = Guid.Empty,
                    IsRequired = true
                },
                ["team-members"] = new NoxActionInput
                {
                    Id = "team-members",
                    Description = "a Comma delimited string containing the usernames of the developers to add to the project",
                    Default = string.Empty,
                    IsRequired = true
                },
            }
        };
    }
    
    private GraphHttpClient? _graphClient;
    private Guid? _projectId;
    private string? _members;
    private bool _isServerContext = false;

    public async Task BeginAsync(IDictionary<string, object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectId = inputs.Value<Guid>("project-id");
        _members = inputs.Value<string>("team-members");
        _graphClient = await connection!.GetClientAsync<GraphHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_graphClient == null || 
            _projectId == null ||
            _projectId == Guid.Empty ||
            string.IsNullOrEmpty(_members))
        {
            ctx.SetErrorMessage("The devops add-team-members action was not initialized");
        }
        else
        {
            try
            {
                var result = await AddTeamMembers(ctx);
                if (result) ctx.SetState(ActionState.Success);
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
        if (!_isServerContext) _graphClient?.Dispose();
        return Task.CompletedTask;
    }

    private async Task<bool> AddTeamMembers(INoxWorkflowContext ctx)
    {
        List<GraphGroup> graphGroups = new();

        GraphGroup? graphGroup = null;

        var projectDescriptor = await _graphClient!.GetDescriptorAsync(_projectId!.Value);
        var groupsInGraph = await _graphClient!.ListGroupsAsync(projectDescriptor.Value);
        
        foreach (var group in groupsInGraph.GraphGroups)
        {
            graphGroups.Add(group);
        }

        while (groupsInGraph.ContinuationToken is not null)
        {
            groupsInGraph = await _graphClient.ListGroupsAsync(continuationToken: groupsInGraph.ContinuationToken.FirstOrDefault());
            foreach (var group in groupsInGraph.GraphGroups)
            {
                graphGroups.Add(group);
            }
        }

        graphGroup = graphGroups.FirstOrDefault(g => g.Description.Contains($"default", StringComparison.OrdinalIgnoreCase) && g.Description.Contains($"project team", StringComparison.OrdinalIgnoreCase));
        
        if (graphGroup == null || _graphClient == null)
        {
            ctx.SetState(ActionState.Error);
            ctx.SetErrorMessage($"Unable to find the default project Team' that should automatically have been created with the project");
            return false;
        }
 
        var usersInGraph = _graphClient.ListUsersAsync(new string[] {"aad"}).Result;

        var members = _members!.Split(',').ToList();
        
        while (usersInGraph.ContinuationToken is not null)
        {
            foreach (var user in usersInGraph.GraphUsers.OrderBy(u => u.DisplayName))
            {
                var developer = members!.FirstOrDefault(d => d.Equals(user.PrincipalName, StringComparison.OrdinalIgnoreCase));
    
                if (developer != null)
                {
                    var isUserInGroup = await _graphClient.CheckMembershipExistenceAsync(user.Descriptor, graphGroup.Descriptor);
                    if (!isUserInGroup)
                    {
                        var membership = await _graphClient.AddMembershipAsync(user.Descriptor, graphGroup.Descriptor);
                    }
                }
            }
            usersInGraph = await _graphClient.ListUsersAsync(new string[] {"aad"}, continuationToken: usersInGraph.ContinuationToken.FirstOrDefault());
        }

        return true;

    }
}