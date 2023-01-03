using Nox.Cli.Actions;
using Npgsql;
using System.Data;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsConnect_v1 : NoxAction
{
    public override NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/connect@v1",
            Author = "Jan Schutte",
            Description = "Connect to Azure Devops",

            Inputs =
            {
                ["server"] = new NoxActionInput { 
                    Id = "server", 
                    Description = "The DevOps server hostname or IP",
                    Default = "localhost",
                    IsRequired = true
                },

                
                ["personalAccessToken"] = new NoxActionInput {
                    Id = "pat",
                    Description = "The personal access token to connect with",
                    Default = string.Empty,
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["connection"] = new NoxActionOutput {
                    Id = "connection",
                    Description = "The connection to the devops project",
                },
            }
        };
    }

    private VssConnection? _connection;

    public override Task BeginAsync(NoxWorkflowExecutionContext ctx, IDictionary<string,object> inputs)
    {
        var server = (string)inputs["server"];
        var pat = (string)inputs["personalAccessToken"];

        _connection = new VssConnection(new Uri(server), new VssBasicCredential(string.Empty, pat));
        return Task.CompletedTask;
    }

    public override async Task<IDictionary<string, object>> ProcessAsync(NoxWorkflowExecutionContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        _state = ActionState.Error;

        if (_connection == null)
        {
            _errorMessage = "The devops connect action was not initialized";
        }
        else
        {
            if (!_connection.HasAuthenticated)
            {
                try
                {
                    await _connection.ConnectAsync();
 
                    outputs["connection"] = _connection;

                    _state = ActionState.Success;
                }
                catch (Exception ex)
                {
                    _errorMessage = ex.Message;
                }
            }
        }

        return outputs;
    }

    public override Task EndAsync(NoxWorkflowExecutionContext ctx)
    {
        if (_connection != null)
        {
            _connection.Disconnect();
            _connection.Dispose();
        }

        return Task.CompletedTask;
    }
}

