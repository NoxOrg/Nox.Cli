using System.Net;
using Azure;
using Azure.ResourceManager.PrivateDns;
using Azure.ResourceManager.PrivateDns.Models;
using Azure.ResourceManager.Resources;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.AzureAd;

public class ArmCreatePrivateDnsRecord_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "arm/create-private-dns-record@v1",
            Author = "Jan Schutte",
            Description = "Create a private DNS record",

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
                    Description = "The name of the Resource Group in which to create the record",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["zone-name"] = new NoxActionInput
                {
                    Id = "zone-name",
                    Description = "The name of the DNS zone in which to create the record",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["record-name"] = new NoxActionInput
                {
                    Id = "record-name",
                    Description = "The name of the DNS record to create",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["record-type"] = new NoxActionInput
                {
                    Id = "record-type",
                    Description = "The type of the DNS record to create",
                    Default = "A",
                    IsRequired = true
                },
                
                ["ip-address"] = new NoxActionInput
                {
                    Id = "ip-address",
                    Description = "The IP address of the DNS record to create",
                    Default = "10.232.144.29",
                    IsRequired = true
                },
            }
        };
    }

    private SubscriptionResource? _sub;
    private string? _rgName;
    private string? _zoneName;
    private string? _recordName;
    private string? _recordType;
    private string? _ipAddress;
    private bool _isServerContext = false;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _sub = inputs.Value<SubscriptionResource>("subscription");
        _rgName = inputs.Value<string>("resource-group-name");
        _zoneName = inputs.Value<string>("zone-name");
        _recordName = inputs.Value<string>("record-name");
        _recordType = inputs.ValueOrDefault<string>("record-type", this);
        _ipAddress = inputs.ValueOrDefault<string>("ip-address", this);
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
            string.IsNullOrWhiteSpace(_recordType) ||
            string.IsNullOrWhiteSpace(_ipAddress))
        {
            ctx.SetErrorMessage("The arm create-private-dns-record action was not initialized");
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
                    var zoneResponse = await rg.GetPrivateDnsZoneAsync(_zoneName);
                    if (zoneResponse.HasValue)
                    {
                        var zone = zoneResponse.Value;
                        switch (_recordType.ToLower())
                        {
                            case "a":
                                try
                                {
                                    var records = zone.GetPrivateDnsARecords();
                                    await records!.CreateOrUpdateAsync(WaitUntil.Completed, _recordName, new PrivateDnsARecordData
                                    {
                                        TtlInSeconds = 300,
                                        PrivateDnsARecords = { new PrivateDnsARecordInfo
                                        {
                                            IPv4Address = IPAddress.Parse(_ipAddress)
                                        }}
                                    });
                                    ctx.SetState(ActionState.Success);
                                }
                                catch(Exception ex)
                                {
                                    ctx.SetErrorMessage(ex.Message);
                                }

                                break;
                        }
                        
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