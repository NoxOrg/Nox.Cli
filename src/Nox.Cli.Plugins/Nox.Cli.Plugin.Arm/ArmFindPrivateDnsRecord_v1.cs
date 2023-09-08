using Azure;
using Azure.ResourceManager.PrivateDns;
using Azure.ResourceManager.Resources;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.AzureAd;

public class ArmFindPrivateDnsRecord_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "arm/find-private-dns-record@v1",
            Author = "Jan Schutte",
            Description = "Find a private DNS record",

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
                    Description = "The name of the Resource Group in which to find the record",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["zone-name"] = new NoxActionInput
                {
                    Id = "zone-name",
                    Description = "The name of the DNS zone in which to find the record",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["record-name"] = new NoxActionInput
                {
                    Id = "record-name",
                    Description = "The name of the DNS record to find",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["record-type"] = new NoxActionInput
                {
                    Id = "record-type",
                    Description = "The type of the DNS record to find",
                    Default = "A",
                    IsRequired = true
                },
                
            },

            Outputs =
            {
                ["is-found"] = new NoxActionOutput
                {
                    Id = "is-found",
                    Description = "Boolean indicating if the private dns was found or not.",
                }
            }
        };
    }

    private SubscriptionResource? _sub;
    private string? _rgName;
    private string? _zoneName;
    private string? _recordName;
    private string? _recordType;
    private bool _isServerContext = false;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _sub = inputs.Value<SubscriptionResource>("subscription");
        _rgName = inputs.Value<string>("resource-group-name");
        _zoneName = inputs.Value<string>("zone-name");
        _recordName = inputs.Value<string>("record-name");
        _recordType = inputs.ValueOrDefault<string>("record-type", this);
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_sub == null || 
            string.IsNullOrEmpty(_rgName) ||
            string.IsNullOrEmpty(_zoneName) ||
            string.IsNullOrWhiteSpace(_recordName) ||
            string.IsNullOrWhiteSpace(_recordType))
        {
            ctx.SetErrorMessage("The arm find-private-dns-record action was not initialized");
        }
        else
        {
            try
            {
                outputs["is-found"] = false;
                var rg = await GetResourceGroup();
                if (rg == null)
                {
                    ctx.SetErrorMessage("Unable to connect to the specified Resource Group");
                }
                else
                {
                    var zoneResponse = await rg.GetPrivateDnsZoneAsync(_zoneName);
                    if (zoneResponse is { HasValue: true })
                    {
                        var zone = zoneResponse.Value;
                        switch (_recordType.ToLower())
                        {
                            case "a":
                                try
                                {
                                    var record = await zone.GetPrivateDnsARecordAsync(_recordName);
                                    outputs["is-found"] = true; 
                                }
                                catch
                                {
                                    //ignore
                                }
                                
                                break;
                        }
                    }
                    ctx.SetState(ActionState.Success);
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