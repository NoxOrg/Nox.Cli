using System.Net.NetworkInformation;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.Variables;

namespace Nox.Cli.Plugins.Network;

public class NetworkPing_v1 : INoxCliAddin
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
                    Default = string.Empty,
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

    public Task BeginAsync(IDictionary<string, IVariable> inputs)
    {
        _host = inputs.ValueOrDefault<string>("host", this);
        if (Uri.IsWellFormedUriString(_host, UriKind.Absolute))
        {
            var uri = new Uri(_host);
            _host = uri.Host;
        }

        _ping = new Ping();

        return Task.FromResult(true);
    }

    public async Task<IDictionary<string, IVariable>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, IVariable>();

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

                    outputs["roundtrip-time"] = new Variable(reply.RoundtripTime);
                }

            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage( ex.Message );
            }
        }

        return outputs;
    }

    public Task EndAsync()

    {
        if (_ping != null)
        {
            _ping.SendAsyncCancel();
        }

        return Task.FromResult(true);
    }
}

