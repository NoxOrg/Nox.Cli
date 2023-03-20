using Microsoft.Graph;
using Microsoft.Graph.Models;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugin.Teams;

public class TeamsAddMember_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "teams/add-members@v1",
            Author = "Jan Schutte",
            Description = "Add AAD users to an MS Teams team, using their AAD objectId",

            Inputs =
            {
                ["teams-client"] = new NoxActionInput
                {
                    Id = "teams-client",
                    Description = "The Teams client, normally this is the result of teams/connect@v1",
                    Default = new GraphServiceClient(new HttpClient()),
                    IsRequired = true
                },
                
                ["team-id"] = new NoxActionInput {
                    Id = "team-id",
                    Description = "The AAD Id of the team",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["object-id"] = new NoxActionInput
                {
                    Id = "object-id",
                    Description = "The AAD object id of the user to tadd to the team.",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["is-owner"] = new NoxActionInput {
                    Id = "is-owner",
                    Description = "Indicator set if the users being added are to be owners of the team",
                    Default = false,
                    IsRequired = false
                }
            }
        };
    }

    private GraphServiceClient? _teamsClient;
    private string? _teamId;
    private string? _objectId;
    private bool? _isOwner;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _teamsClient = inputs.Value<GraphServiceClient>("teams-client");
        _teamId = inputs.Value<string>("team-id");
        _objectId = inputs.Value<string>("object-id");
        _isOwner = inputs.ValueOrDefault<bool>("is-owner", this);
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_teamsClient == null || 
            string.IsNullOrEmpty(_teamId) ||
            string.IsNullOrEmpty(_objectId) ||
            _isOwner == null)
        {
            ctx.SetErrorMessage("The Teams add-member action was not initialized");
        }
        else
        {
            try
            {
                List<string>? roles = null;
                if (_isOwner == true)
                {
                    roles.Add("owner");
                }

                var request = new ConversationMember
                {
                    OdataType = "#microsoft.graph.aadUserConversationMember",
                    Roles = roles,
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "user@odata.bind", $"https://graph.microsoft.com/v1.0/users('{_objectId}')" }
                    }
                };
                var response = await _teamsClient.Teams[_teamId].Members.PostAsync(request);
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