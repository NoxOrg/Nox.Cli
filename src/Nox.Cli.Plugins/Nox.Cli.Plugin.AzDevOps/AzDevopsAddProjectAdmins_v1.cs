using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsAddProjectAdmins_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/add-project-admins@v1",
            Author = "Jan Schutte",
            Description = "Add a team member as an administrator on a DevOps project",

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
                ["admins"] = new NoxActionInput
                {
                    Id = "admins",
                    Description = "A comma delimited list of usernames of the users to add as administrators on the project",
                    Default = string.Empty,
                    IsRequired = true
                }
            }
        };
    }
    
    private GraphHttpClient? _graphClient;
    private string? _projectName;
    private string? _admins;
    private bool _isServerContext = false;

    public async Task BeginAsync(IDictionary<string, object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectName = inputs.Value<string>("project-name");
        _admins = inputs.Value<string>("admins");
        _graphClient = await connection!.GetClientAsync<GraphHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_graphClient == null || 
            string.IsNullOrEmpty(_projectName) ||  
            string.IsNullOrEmpty(_admins))
        {
            ctx.SetErrorMessage("The devops add-project-admins action was not initialized");
        }
        else
        {
            try
            {
                var adminList = _admins.Split(',').ToList();
                if (adminList.Count == 0)
                {
                    ctx.SetErrorMessage("No usernames found to add!");
                }
                else
                {
                    if (await AddAdmins(ctx, adminList)) ctx.SetState(ActionState.Success);
                }
                
                
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

    private async Task<bool> AddAdmins(INoxWorkflowContext ctx, List<string> usernames)
    {
        var tries = 0;

        List<GraphGroup> graphGroups = new();

        GraphGroup? graphGroup = null;

        while (tries++ <= 2)
        {
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

            graphGroup = graphGroups.FirstOrDefault(g => g.PrincipalName.Contains($"\\Project Administrators", StringComparison.OrdinalIgnoreCase));

            if (graphGroup == null)
            {
                // try again;
                await Task.Delay(3000);
                continue;
            }

            break;
        }

        if (graphGroup == null || _graphClient == null)
        {
            ctx.SetState(ActionState.Error);
            ctx.SetErrorMessage($"Unable to find group '\\Project Administrators' that should automatically have been created with the project");
            return false;
        }
 
        var usersInGraph = _graphClient.ListUsersAsync(new string[] {"aad"}).Result;

        while (usersInGraph.ContinuationToken is not null)
        {
            foreach (var user in usersInGraph.GraphUsers.OrderBy(u => u.DisplayName))
            {
                var developer = usernames!.FirstOrDefault(d => d.Equals(user.PrincipalName, StringComparison.OrdinalIgnoreCase));
    
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