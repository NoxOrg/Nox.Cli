using Microsoft.Graph;
using Microsoft.Graph.Models;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugins.AzDevops;

public class AzureAdFindGroup_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azuread/find-group@v1",
            Author = "Jan Schutte",
            Description = "Find an Azure Active Directory group using the group name",

            Inputs =
            {
                ["aad-client"] = new NoxActionInput
                {
                    Id = "aad-client",
                    Description = "The AAD client",
                    Default = new GraphServiceClient(new HttpClient()),
                    IsRequired = true
                },
                
                ["group-name"] = new NoxActionInput
                {
                    Id = "group-name",
                    Description = "The name of the aad group to create",
                    Default = string.Empty,
                    IsRequired = true
                }
            },

            Outputs =
            {
                ["group-id"] = new NoxActionOutput
                {
                    Id = "group-id",
                    Description = "The Id of the AAD group that was searched. Will return null if group is not found.",
                },
            }
        };
    }

    private string? _groupName;
    private GraphServiceClient? _aadClient;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _groupName = inputs.Value<string>("group-name");
        _aadClient = inputs.Value<GraphServiceClient>("aad-client");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_aadClient == null || string.IsNullOrEmpty(_groupName))
        {
            ctx.SetErrorMessage("The az active directory find-group action was not initialized");
        }
        else
        {
            try
            {
                var projectGroupName = _groupName.ToUpper();

                var groups = await _aadClient.Groups.GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Count = true;
                    requestConfiguration.QueryParameters.Filter = $"DisplayName eq '{projectGroupName}'";
                });
                
                if (groups != null &&  groups.Value!.Count == 1)
                {
                    outputs["group-id"] = groups.Value.First().Id!;
                }
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