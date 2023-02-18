using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDevopsConnect_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
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

                
                ["personal-access-token"] = new NoxActionInput {
                    Id = "personal-access-token",
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

    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        var server = inputs.Value<string>("server");
        var pat = inputs.Value<string>("personal-access-token");
        if(server != null && pat != null)
        {
            // make sure a malicious url is not being injected to obtain PAT
            if(server.Trim().ToLower().StartsWith("https://dev.azure.com/"))
            {
                _connection = new VssConnection(new Uri(server), new VssBasicCredential(string.Empty, pat));
            }      
        }
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_connection == null)
        {
            ctx.SetErrorMessage("The devops connect action was not initialized");
        }
        else
        {
            if (!_connection.HasAuthenticated)
            {
                try
                {
                    await _connection.ConnectAsync();
                    
                    outputs["connection"] = _connection;

                    ctx.SetState(ActionState.Success);
                }
                catch (Exception ex)
                {
                    ctx.SetErrorMessage(ex.Message);
                }
            }
        }

        return outputs;
    }

    public Task EndAsync()
    {
        if (_connection != null)
        {
            _connection.Disconnect();
            _connection.Dispose();
        }

        return Task.CompletedTask;
    }
}

