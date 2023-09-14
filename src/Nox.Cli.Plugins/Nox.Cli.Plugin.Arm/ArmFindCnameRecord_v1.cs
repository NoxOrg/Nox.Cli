using Azure;
using Azure.ResourceManager.Dns;

using Azure.ResourceManager.Resources;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.AzureAd;

public class ArmFindCnameRecord_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "arm/find-cname-record@v1",
            Author = "Jan Schutte",
            Description = "Find a CNAME record",

            Inputs =
            {
                ["subscription"] = new NoxActionInput {
                    Id = "subscription",
                    Description = "The Azure Subscription connected to with action 'arm/connect@v1'",
                    Default = null!,
                    IsRequired = true
                },
                ["resource-group-name"] = new NoxActionInput {
                    Id = "resource-group-name",
                    Description = "The name of the Resource Group in which to find the CNAME record",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["zone-name"] = new NoxActionInput
                {
                    Id = "zone-name",
                    Description = "The name of the DNS zone in which to find the CNAME record",
                    Default = string.Empty,
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["is-found"] = new NoxActionOutput
                {
                    Id = "is-found",
                    Description = "Boolean indicating if the record was found or not.",
                }
            }
        };
    }

    private SubscriptionResource? _sub;
    private string? _rgName;
    private string? _zoneName;
    private bool _isServerContext = false;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _sub = inputs.Value<SubscriptionResource>("subscription");
        _rgName = inputs.Value<string>("resource-group-name");
        _zoneName = inputs.Value<string>("zone-name");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_sub == null || 
            string.IsNullOrEmpty(_rgName) ||
            string.IsNullOrEmpty(_zoneName))
        {
            ctx.SetErrorMessage("The arm find-cname-record action was not initialized");
        }
        else
        {
            try
            {
                
                var rg = await GetResourceGroup();
                if (rg == null)
                {
                    ctx.SetErrorMessage("Unable to connect to the specified Resource Group");
                }
                else
                {
                    var zoneResponse = await rg.GetDnsZoneAsync(_zoneName);
                    if (zoneResponse.HasValue)
                    {
                        var zone = zoneResponse.Value;
                        
                    }
                    else
                    {
                        ctx.SetErrorMessage($"Unable to locate the zone: {_zoneName} in resource group: {_rgName}");
                    }
                }
              
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
    
    private async Task<ResourceGroupResource?> GetResourceGroup()
    {
        var rgResponse = await _sub!.GetResourceGroupAsync(_rgName);
        return rgResponse.HasValue ? rgResponse.Value : null;
    }
}