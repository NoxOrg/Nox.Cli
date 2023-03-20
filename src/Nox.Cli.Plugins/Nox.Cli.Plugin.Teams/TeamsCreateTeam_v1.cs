using Microsoft.Graph;
using Microsoft.Graph.Models;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugin.Teams;

public class TeamsCreateTeam_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "teams/create-team@v1",
            Author = "Jan Schutte",
            Description = "Create a new MS Teams, team",

            Inputs =
            {
                ["teams-client"] = new NoxActionInput
                {
                    Id = "teams-client",
                    Description = "The Teams client, normally this is the result of teams/connect@v1",
                    Default = new GraphServiceClient(new HttpClient()),
                    IsRequired = true
                },
                ["team-name"] = new NoxActionInput {
                    Id = "team-name",
                    Description = "The name of the team",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["team-description"] = new NoxActionInput {
                    Id = "team-description",
                    Description = "The description of the team",
                    Default = string.Empty,
                    IsRequired = true
                }
            },

            Outputs =
            {
                ["team-id"] = new NoxActionOutput
                {
                    Id = "team-id",
                    Description = "The Team-id of the created team."
                },
            }
        };
    }

    private GraphServiceClient? _teamsClient;
    private string? _teamName;
    private string? _teamDescription;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _teamsClient = inputs.Value<GraphServiceClient>("teams-client");
        _teamName = inputs.Value<string>("team-name");
        _teamDescription = inputs.Value<string>("team-description");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_teamsClient == null || 
            string.IsNullOrEmpty(_teamName) ||
            string.IsNullOrEmpty(_teamDescription))
        {
            ctx.SetErrorMessage("The Teams create-team action was not initialized");
        }
        else
        {
            try
            {
                var requestBody = new Team
                {
                    DisplayName = _teamName,
                    Description = _teamDescription,
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "template@odata.bind", "https://graph.microsoft.com/v1.0/teamsTemplates('standard')" }
                    }
                };
                var result = await _teamsClient.Teams.PostAsync(requestBody);
                if (result != null)
                {
                    outputs["team-id"] = result.Id!;    
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