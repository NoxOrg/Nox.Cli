using Azure.Identity;
using Microsoft.Graph;
using Nox.Cli.Actions;
using ActionState = Nox.Cli.Actions.ActionState;

namespace Nox.Cli.Plugins.AzDevops;

public class AzureAdConnect_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azuread/connect@v1",
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

    public Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string, object> inputs)
    {
        _tenantId = (string)inputs["tenant-id"];
        _clientId = (string)inputs["client-id"];
        _clientSecret = (string)inputs["client-secret"];
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        try
        {
            var userScopes = new string[] { @"https://graph.microsoft.com/.default" };
            var credentials = new ClientSecretCredential(_tenantId, _clientId, _clientSecret);
            var client = new GraphServiceClient(credentials, userScopes);
            outputs["aad-client"] = client;
            ctx.SetState(ActionState.Success);
        }
        catch (Exception ex)
        {
            ctx.SetErrorMessage(ex.Message);
        }

        return Task.FromResult((IDictionary<string, object>)outputs);
    }

    public Task EndAsync(INoxWorkflowContext ctx)
    {
        return Task.CompletedTask;
    }
}