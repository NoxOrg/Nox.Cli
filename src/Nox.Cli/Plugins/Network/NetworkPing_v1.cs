using Nox.Cli.Actions;
using System.Net.NetworkInformation;

namespace Nox.Cli.Plugins.Network;

public class NetworkPing_v1 : NoxAction
{
    public override NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "network/ping@v1",
            Author = "Andre Sharpe",
            Description = "Pings a hostname or IP address",

            Inputs =
            {
                ["host"] = new NoxActionInput {
                    Id = "host",
                    Description = "The hostname or IP address",
                    Default = "localhost",
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["roundtrip-time"] = new NoxActionOutput {
                    Id = "roundtrip-time",
                    Description = "The hostname or IP address",
                    Value = 0L,
                },
            },
        };
    }

    private Ping? _ping;

    private string? _host;

    public override Task BeginAsync(NoxWorkflowExecutionContext ctx, IDictionary<string, object> inputs)
    {
        _host = (string)inputs["host"];

        _ping = new Ping();

        return Task.FromResult(true);
    }

    public override async Task<IDictionary<string, object>> ProcessAsync(NoxWorkflowExecutionContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        _state = ActionState.Error;

        if (_ping == null)
        {
            _errorMessage = "The ping action was not initialized";
        }
        else
        {
            try
            {
                var reply = await _ping.SendPingAsync(_host!);

                if (reply.Status == IPStatus.Success)
                {
                    _state = ActionState.Success;

                    outputs["roundtrip-time"] = reply.RoundtripTime;
                }

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
        if (_ping != null)
        {
            _ping.SendAsyncCancel();
        }

        return Task.FromResult(true);
    }
}

