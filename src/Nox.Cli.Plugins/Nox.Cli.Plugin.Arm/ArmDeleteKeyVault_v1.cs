using Azure;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.Arm;

public class ArmDeleteKeyVault_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "arm/delete-key-vault@v1",
            Author = "Jan Schutte",
            Description = "Delete an Azure key vault",

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
                    Description = "The name of the key vault to delete",
                    Default = string.Empty,
                    IsRequired = true
                }
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
            ctx.SetErrorMessage("The arm delete-key-vault action was not initialized");
        }
        else
        {
            try
            {
                
                var resourceGroups = _sub.GetResourceGroups();
                var resourceGroupResponse = await resourceGroups.GetAsync(_rgName);
                if (resourceGroupResponse.HasValue)
                {
                    var resourceGroup = resourceGroupResponse.Value;
                    var vaults = resourceGroup.GetKeyVaults();
                    var vaultResponse = await vaults.GetAsync(_kvName);
                    if (vaultResponse.HasValue)
                    {
                        var vault = vaultResponse.Value;
                        await vault.DeleteAsync(WaitUntil.Completed);
                        var deletedKvResponse = await _sub.GetDeletedKeyVaultAsync(resourceGroup.Data.Location, _kvName);
                        if (deletedKvResponse.HasValue)
                        {
                            var deletedKv = deletedKvResponse.Value;
                            await deletedKv.PurgeDeletedAsync(WaitUntil.Completed);
                        }
                        ctx.SetState(ActionState.Success);
                    }
                    else
                    {
                        ctx.SetErrorMessage($"Key vault {_kvName} does not exist.");
                    }
                }
                else
                {
                    ctx.SetErrorMessage("Unable to connect to the specified Resource Group");
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
}