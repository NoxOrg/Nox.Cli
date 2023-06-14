using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugin.AzureAd;

public class AzureAdFindCnameRecord_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azuread/find-dns-record@v1",
            Author = "Jan Schutte",
            Description = "Find an Azure Active Directory CName DNS record",

            Inputs =
            {
                ["aad-client"] = new NoxActionInput
                {
                    Id = "aad-client",
                    Description = "The AAD client",
                    Default = new GraphServiceClient(new HttpClient()),
                    IsRequired = true
                },
                
                ["resource-group-name"] = new NoxActionInput
                {
                    Id = "resource-group-name",
                    Description = "The name of the aad resource group in which to add the DNS record",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["zone-name"] = new NoxActionInput
                {
                    Id = "zone-name",
                    Description = "The name of the aad DNS zone in which to add the DNS record",
                    Default = string.Empty,
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["is-found"] = new NoxActionOutput
                {
                    Id = "is-found",
                    Description = "Boolean indicating if the group was found or not.",
                },
                ["group-id"] = new NoxActionOutput
                {
                    Id = "group-id",
                    Description = "The Id of the AAD group that was searched. Will return null if group is not found.",
                },
            }
        };
    }

    private string? _resourceGroupName;
    private string? _zoneName;
    private GraphServiceClient? _aadClient;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _resourceGroupName = inputs.Value<string>("resource-group-name");
        _zoneName = inputs.Value<string>("zone-name");
        _aadClient = inputs.Value<GraphServiceClient>("aad-client");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_aadClient == null || 
            string.IsNullOrEmpty(_resourceGroupName) ||
            string.IsNullOrEmpty(_zoneName))
        {
            ctx.SetErrorMessage("The az active directory find-cname-record action was not initialized");
        }
        else
        {
            try
            {
                outputs["is-found"] = false;

                var groups = await _aadClient.Groups.GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Count = true;
                    requestConfiguration.QueryParameters.Filter = $"DisplayName eq '{_resourceGroupName}'";
                    requestConfiguration.QueryParameters.Select = new[] { "id" };
                });
                
                if (groups != null &&  groups.Value!.Count == 1)
                {
                    
                    ctx.SetState(ActionState.Success);
                }
                
            }
            catch (ODataError odataError)
            {
                ctx.SetErrorMessage(odataError.Error!.Message!);
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