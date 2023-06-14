using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugin.Teams;

public class TeamsCreateChannel_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "teams/create-channel@v1",
            Author = "Jan Schutte",
            Description = "Create a new MS Teams, channel",

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
                    Description = "The id of the team",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["channel-name"] = new NoxActionInput {
                    Id = "channel-name",
                    Description = "The name of the channel to create",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["channel-description"] = new NoxActionInput {
                    Id = "channel-description",
                    Description = "The description of the channel to create",
                    Default = string.Empty,
                    IsRequired = true
                }
            },

            Outputs =
            {
                ["channel-id"] = new NoxActionOutput
                {
                    Id = "channel-id",
                    Description = "The Id of the new channel."
                },
            }
        };
    }

    private GraphServiceClient? _aadClient;
    private string? _teamId;
    private string? _channelName;
    private string? _channelDescription;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _aadClient = inputs.Value<GraphServiceClient>("aad-client");
        _teamId = inputs.Value<string>("team-id");
        _channelName = inputs.Value<string>("channel-name");
        _channelDescription = inputs.Value<string>("channel-description");
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
            ctx.SetErrorMessage("The Teams create-channel action was not initialized");
        }
        else
        {
            try
            {
                if (string.IsNullOrEmpty(_channelDescription)) _channelDescription = $"This channel is used for {_channelName}";

                var request = new Channel
                {
                    DisplayName = _channelName,
                    Description = _channelDescription,
                    MembershipType = ChannelMembershipType.Standard
                };
                var response = await _aadClient.Teams[_teamId].Channels.PostAsync(request);
                if (response != null && !string.IsNullOrEmpty(response.Id))
                {
                    outputs["channel-id"] = response.Id;
                    
                };
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