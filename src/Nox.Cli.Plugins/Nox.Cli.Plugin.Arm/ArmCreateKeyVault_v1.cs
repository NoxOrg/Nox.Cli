using Azure;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.Arm;

public class ArmCreateKeyVault_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "arm/create-key-vault@v1",
            Author = "Jan Schutte",
            Description = "Create an Azure key vault",

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
                    Description = "The name of the Resource Group in which to find the Key Vault",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["key-vault-name"] = new NoxActionInput {
                    Id = "key-vault-name",
                    Description = "The name of the key vault to find",
                    Default = string.Empty,
                    IsRequired = true
                }
            },
            
            Outputs =
            {
                ["key-vault"] = new NoxActionOutput {
                    Id = "key-vault",
                    Description = "The Azure Key Vault instance that was created.",
                },
            }
        };
    }

    private SubscriptionResource? _sub;
    private string? _rgName;
    private string? _kvName;
    private bool _isServerContext = false;

    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _sub = inputs.Value<SubscriptionResource>("subscription");
        _rgName = inputs.Value<string>("resource-group-name");
        _kvName = inputs.Value<string>("key-vault-name");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_sub == null || 
            string.IsNullOrEmpty(_rgName) ||
            string.IsNullOrEmpty(_kvName))
        {
            ctx.SetErrorMessage("The arm create-key-vault action was not initialized");
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
                    if (await CreateVault(rg) != null)
                    {
                        ctx.SetState(ActionState.Success);    
                    }
                    else
                    {
                        ctx.SetErrorMessage("Unable to create the specified Key Vault!");
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
        var rgs = _sub!.GetResourceGroups();
        var rgResponse = await rgs.GetAsync(_rgName);
        return rgResponse.HasValue ? rgResponse.Value : null;
    }

    private async Task<KeyVaultResource?> CreateVault(ResourceGroupResource rg)
    {
        var vaults = rg.GetKeyVaults();
        var location = rg.Data.Location;
        var sku = new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard);
        var kvProps = new KeyVaultProperties(_sub!.Data.TenantId!.Value, sku)
        {
            CreateMode = KeyVaultCreateMode.Default,
            TenantId = _sub.Data.TenantId.Value,
            EnablePurgeProtection = true,
            EnableSoftDelete = true,
        };
        await vaults.CreateOrUpdateAsync(WaitUntil.Completed, _kvName, new KeyVaultCreateOrUpdateContent(location, kvProps));
        var vaultResponse = await vaults.GetAsync(_kvName);
        return vaultResponse.HasValue ? vaultResponse.Value : null;
    }
}