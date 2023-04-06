using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugin.Teams;

public class TeamsSendChannelMessage_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "teams/send-channel-message@v1",
            Author = "Jan Schutte",
            Description = "Send a message to a MS Teams channel.",

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
                    Description = "The AAD Id of the team",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["channel-id"] = new NoxActionInput {
                    Id = "channel-id",
                    Description = "The AAD Id of the channel",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["message-body"] = new NoxActionInput
                {
                    Id = "message-body",
                    Description = "The body of the message to send.",
                    Default = string.Empty,
                    IsRequired = true
                }
            }
        };
    }

    private GraphServiceClient? _aadClient;
    private string? _teamId;
    private string? _channelId;
    private string? _messageBody;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _aadClient = inputs.Value<GraphServiceClient>("aad-client");
        _teamId = inputs.Value<string>("team-id");
        _channelId = inputs.Value<string>("channel-id");
        _messageBody = inputs.Value<string>("message-body");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_aadClient == null || 
            string.IsNullOrEmpty(_channelId) ||
            string.IsNullOrEmpty(_teamId) ||
            string.IsNullOrEmpty(_messageBody))
        {
            ctx.SetErrorMessage("The Teams send-channel-message action was not initialized");
        }
        else
        {
            try
            {
                var request = new ChatMessage
                {
                    Body = new ItemBody
                    {
                        Content = _messageBody
                    }
                };
                var response = await _aadClient.Teams[_teamId].Channels[_channelId].Messages.PostAsync(request);
                    
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