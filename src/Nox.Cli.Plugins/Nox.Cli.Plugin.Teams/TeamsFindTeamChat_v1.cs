using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugin.Teams;

public class TeamsFindTeamChat_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "teams/find-chat@v1",
            Author = "Jan Schutte",
            Description = "Find a chat in an MS Teams, team",

            Inputs =
            {
                ["aad-client"] = new NoxActionInput
                {
                    Id = "aad-client",
                    Description = "The AAD client, typically the result of azuread/connect@v1",
                    Default = new GraphServiceClient(new HttpClient()),
                    IsRequired = true
                },

                ["team-id"] = new NoxActionInput {
                    Id = "team-id",
                    Description = "The Azure AD Id of the team",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["topic"] = new NoxActionInput {
                    Id = "topic",
                    Description = "The topic of the chat to find",
                    Default = string.Empty,
                    IsRequired = true
                }
            },

            Outputs =
            {
                ["is-found"] = new NoxActionOutput
                {
                    Id = "is-found",
                    Description = "Boolean indicating if the channel was found or not.",
                },
                ["chat-id"] = new NoxActionOutput
                {
                    Id = "chat-id",
                    Description = "The Id of the found chat. This will be null if the chat does not exist."
                },
            }
        };
    }

    private GraphServiceClient? _aadClient;
    private string? _teamId;
    private string? _topic;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _aadClient = inputs.Value<GraphServiceClient>("aad-client");
        _teamId = inputs.Value<string>("team-id");
        _topic = inputs.Value<string>("topic");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_aadClient == null || 
            string.IsNullOrEmpty(_teamId) ||
            string.IsNullOrEmpty(_topic))
        {
            ctx.SetErrorMessage("The Teams find-chat action was not initialized");
        }
        else
        {
            try
            {
                outputs["is-found"] = false;
                var chats = await _aadClient.Chats.GetAsync(config =>
                {
                    config.QueryParameters.Filter = $"topic eq {_topic}";
                    config.QueryParameters.Select = new[] { "id" };
                });

                
                
                ctx.SetState(ActionState.Success);
            }
            catch (ODataError odataError)
            {
                ctx.SetErrorMessage(odataError.Error!.Message!);
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