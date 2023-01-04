using Nox.Cli.Actions;
using System.Net.NetworkInformation;

namespace Nox.Cli.Plugins.Network;

public class NetworkPing_v1 : INoxActionProvider
{
    public NoxActionMetaData Discover()
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

    public Task BeginAsync(INoxWorkflowExecutionContext ctx, IDictionary<string, object> inputs)
    {
        _host = (string)inputs["host"];

        _ping = new Ping();

        return Task.FromResult(true);
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowExecutionContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_ping == null)
        {
            ctx.SetErrorMessage("The ping action was not initialized");
        }
        else
        {
            try
            {
                var reply = await _ping.SendPingAsync(_host!);

                if (reply.Status == IPStatus.Success)
                {
                    ctx.SetState(ActionState.Success);

                    outputs["roundtrip-time"] = reply.RoundtripTime;
                }

            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage( ex.Message );
            }
        }

        return outputs;
    }

    public Task EndAsync(INoxWorkflowExecutionContext ctx)

    {
        if (_ping != null)
        {
            _ping.SendAsyncCancel();
        }

        return Task.FromResult(true);
    }
}

