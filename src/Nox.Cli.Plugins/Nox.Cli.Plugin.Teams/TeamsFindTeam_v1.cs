using Microsoft.Graph;
using Microsoft.Graph.Connections.Item.Groups;
using Microsoft.Graph.Models;
using Microsoft.Graph.Sites.Item.TermStore.Groups;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugin.Teams;

public class TeamsFindTeam_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "teams/find-team@v1",
            Author = "Jan Schutte",
            Description = "Find an MS Team using the team name",

            Inputs =
            {
                ["aad-client"] = new NoxActionInput
                {
                    Id = "aad-client",
                    Description = "The AAD client, typically the result of azuread/connect@v1",
                    Default = new GraphServiceClient(new HttpClient()),
                    IsRequired = true
                },

                ["team-name"] = new NoxActionInput {
                    Id = "team-name",
                    Description = "The name of the team to find",
                    Default = string.Empty,
                    IsRequired = true
                }
            },

            Outputs =
            {
                ["team-id"] = new NoxActionOutput
                {
                    Id = "team-id",
                    Description = "The Team-id of the team."
                },
            }
        };
    }

    private GraphServiceClient? _aadClient;
    private string? _teamName;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _aadClient = inputs.Value<GraphServiceClient>("aad-client");
        _teamName = inputs.Value<string>("team-name");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_aadClient == null || 
            string.IsNullOrEmpty(_teamName))
        {
            ctx.SetErrorMessage("The Teams find-team action was not initialized");
        }
        else
        {
            try
            {
                var team = await _aadClient.Teams[_teamName].GetAsync();
                if (team != null) outputs["team-id"] = team.Id!;
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