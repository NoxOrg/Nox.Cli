using Azure.Identity;
using Microsoft.Graph;
using Nox.Cli.Actions;
using ActionState = Nox.Cli.Actions.ActionState;

namespace Nox.Cli.Plugins.AzDevops;

public class AzDsConnect_v1 : NoxAction
{
    public override NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azds/connect@v1",
            Author = "Jan Schutte",
            Description = "Connect to Azure Active Directory",

            Inputs =
            {
                ["tenant-id"] = new NoxActionInput
                {
                    Id = "tenant-id",
                    Description = "The AAD Tenant Id",
                    Default = string.Empty,
                    IsRequired = true
                },

                ["client-id"] = new NoxActionInput
                {
                    Id = "client-id",
                    Description = "The AAD Client Id",
                    Default = string.Empty,
                    IsRequired = true
                },

                ["client-secret"] = new NoxActionInput
                {
                    Id = "client-secret",
                    Description = "The AAD Client Secret",
                    Default = string.Empty,
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["aad-client"] = new NoxActionOutput
                {
                    Id = "aad-client",
                    Description = "The AAD client that was created",
                },
            }
        };
    }

    private string? _tenantId;
    private string? _clientId;
    private string? _clientSecret;

    public override Task BeginAsync(NoxWorkflowExecutionContext ctx, IDictionary<string, object> inputs)
    {
        _tenantId = (string)inputs["tenant-id"];
        _clientId = (string)inputs["client-id"];
        _clientSecret = (string)inputs["client-secret"];
        return Task.CompletedTask;
    }

    public override Task<IDictionary<string, object>> ProcessAsync(NoxWorkflowExecutionContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        _state = ActionState.Error;

        try
        {
            var userScopes = new string[] { @"https://graph.microsoft.com/.default" };
            var credentials = new ClientSecretCredential(_tenantId, _clientId, _clientSecret);
            var client = new GraphServiceClient(credentials, userScopes);
            outputs["aad-client"] = client;
            _state = ActionState.Success;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }

        return Task.FromResult((IDictionary<string, object>)outputs);
    }

    public override Task EndAsync(NoxWorkflowExecutionContext ctx)
    {
        return Task.CompletedTask;
    }
}