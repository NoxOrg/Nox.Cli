using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugin.Teams;

public class TeamsCreateTeamChat_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "teams/create-team-chat@v1",
            Author = "Jan Schutte",
            Description = "Create a chat in an MS Teams, team",

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
                
                ["channel-name"] = new NoxActionInput {
                    Id = "channel-name",
                    Description = "The name of the channel to find",
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
                ["channel-id"] = new NoxActionOutput
                {
                    Id = "channel-id",
                    Description = "The Id of the found channel. This will be null if the channel does not exist."
                },
            }
        };
    }

    private GraphServiceClient? _aadClient;
    private string? _teamId;
    private string? _channelName;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _aadClient = inputs.Value<GraphServiceClient>("aad-client");
        _teamId = inputs.Value<string>("team-id");
        _channelName = inputs.Value<string>("channel-name");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_aadClient == null || 
            string.IsNullOrEmpty(_teamId) ||
            string.IsNullOrEmpty(_channelName))
        {
            ctx.SetErrorMessage("The Teams find-channel action was not initialized");
        }
        else
        {
            try
            {
                outputs["is-found"] = false;
                var channels = await _aadClient.Teams[_teamId].Channels.GetAsync((config) =>
                {
                    config.QueryParameters.Filter = $"DisplayName eq '{_channelName}'";
                });

                if (channels != null && channels.Value!.Count == 1)
                {
                    outputs["is-found"] = true;
                    outputs["channel-id"] = channels.Value!.First().Id!;
                }
                
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