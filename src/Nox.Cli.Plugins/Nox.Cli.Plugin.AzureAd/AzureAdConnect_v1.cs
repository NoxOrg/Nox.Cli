using Azure.Identity;
using Microsoft.Graph;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using ActionState = Nox.Cli.Abstractions.ActionState;

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

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _tenantId = inputs.Value<string>("tenant-id");
        _clientId = inputs.Value<string>("client-id");
        _clientSecret = inputs.Value<string>("client-secret");
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
            //var credentials = new DefaultAzureCredential();
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

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }
}