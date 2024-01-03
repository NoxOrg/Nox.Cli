using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

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
                ["port"] = new NoxActionInput {
                    Id = "port",
                    Description = "The port to ping",
                    Default = 80,
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["roundtrip-time"] = new NoxActionOutput {
                    Id = "roundtrip-time",
                    Description = "The time (ms) taken to lookup the host",
                    Value = 0L,
                },
            },
        };
    }

    private TcpClient? _client;

    private string? _host;
    private int? _port;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _host = inputs.ValueOrDefault<string>("host", this);
        _port = inputs.ValueOrDefault<int>("port", this);
        
        if (Uri.IsWellFormedUriString(_host, UriKind.Absolute))
        {
            var uri = new Uri(_host);
            _host = uri.Host;
        }

        _client = new TcpClient();

        return Task.FromResult(true);
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_client == null)
        {
            ctx.SetErrorMessage("The ping action was not initialized");
        }
        else
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                await _client.ConnectAsync(_host!, _port!.Value);
                sw.Stop();
                outputs["roundtrip-time"] = sw.ElapsedMilliseconds;
                ctx.SetState(ActionState.Success);
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
        _client?.Dispose();
        return Task.FromResult(true);
    }
}