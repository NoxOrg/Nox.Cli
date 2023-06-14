using System.Text;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.PersonalAccessToken;
using RestSharp;
using RestSharp.Authenticators.OAuth2;

namespace Nox.Cli.Plugin.AzDevOps;

public class AzDevopsGetPat_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/get-pat@v1",
            Author = "Jan Schutte",
            Description = "Get an Azure Devops Personal Access Token",

            Inputs =
            {
                ["organization"] = new NoxActionInput {
                    Id = "organization",
                    Description = "The Azure DevOps organization to connect to",
                    Default = string.Empty,
                    IsRequired = true
                },

                ["access-token"] = new NoxActionInput { 
                    Id = "access-token", 
                    Description = "An Azure DevOps Api access token",
                    Default = string.Empty,
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["result"] = new NoxActionOutput {
                    Id = "result",
                    Description = "The Azure DevOps Personal Access Token",
                },
                ["base64-result"] = new NoxActionOutput {
                    Id = "base64-result",
                    Description = "The Azure DevOps Personal Access Token in base64 encoding, ready to use with a git command"
                }
            }
        };
    }

    private string? _organization;
    private string? _accessToken;

    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _organization = inputs.Value<string>("organization");
        _accessToken = inputs.Value<string>("access-token");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        if (ctx.IsServer) throw new NoxCliException("This action cannot be executed on a server. remove the run-at-server attribute for this step in your Nox workflow.");
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_organization) ||
            string.IsNullOrEmpty(_accessToken))
        {
            ctx.SetErrorMessage("The devops get-pat action was not initialized");
        }
        else
        {
            try
            {
                var tokenCache = ctx.CacheManager!.TokenCache;
                if (tokenCache == null) throw new NoxCliException("Cannot execute this action without a PersistedTokenCache. Make sure your NoxCliCacheManager builder has a concrete implementation of NoxCliCacheManager in its constructor.");

                var patProvider = new AzDevOpsPatProvider(tokenCache, _organization);
                var pat = await patProvider.GetPat(_accessToken);
                outputs["result"] = pat;
                outputs["base64-result"] = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{pat}"));
                ctx.SetState(ActionState.Success);
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