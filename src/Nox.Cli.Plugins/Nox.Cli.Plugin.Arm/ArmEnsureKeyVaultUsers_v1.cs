using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.Arm;

public class ArmEnsureKeyVaultUsers_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "arm/ensure-key-vault-users@v1",
            Author = "Jan Schutte",
            Description = "Ensure a list of Azure AD users have access to a key vault",

            Inputs =
            {
                ["key-vault"] = new NoxActionInput {
                    Id = "key-vault",
                    Description = "The Azure key vault to add the users to. This is the result from 'arm/find-key-vault@v1' or 'arm/create-key-vault@v1'",
                    Default = null!,
                    IsRequired = true
                },
                ["user-object-id-list"] = new NoxActionInput
                {
                    Id = "user-object-id-list",
                    Description = "a comma separated list of the user object-id's to add to the key vault",
                    Default = new List<string>(),
                    IsRequired = true
                },
                ["is-admin"] = new NoxActionInput
                {
                    Id = "is-admin",
                    Description = "Indicates if the users are to be added as administrators of the key vault. Administrators can delete secrets.",
                    Default = false,
                    IsRequired = true
                }
            }
        };
    }

    private KeyVaultResource? _keyVault;
    private List<string>? _userObjectIds;
    private bool? _isAdmin;
    private bool _isServerContext = false;

    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _keyVault = inputs.Value<KeyVaultResource>("key-vault");
        _userObjectIds = inputs.Value<List<string>>("user-object-ids");
        _isAdmin = inputs.ValueOrDefault<bool>("is-admin", this);
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_keyVault == null || 
            _userObjectIds == null ||
            _userObjectIds.Count == 0 ||
            _isAdmin == null)
        {
            ctx.SetErrorMessage("The arm ensure-key-vault-users action was not initialized");
        }
        else
        {
            try
            {
                IdentityAccessPermissions? permissions;
                if (_isAdmin == true)
                {
                    permissions = new IdentityAccessPermissions
                    {
                        Keys = { IdentityAccessKeyPermission.All },
                        Secrets = { IdentityAccessSecretPermission.All }
                    };
                }
                else
                {
                    permissions = new IdentityAccessPermissions
                    {
                        Secrets = { IdentityAccessSecretPermission.Get, IdentityAccessSecretPermission.List, IdentityAccessSecretPermission.Set },
                        Keys = { IdentityAccessKeyPermission.Create, IdentityAccessKeyPermission.List, IdentityAccessKeyPermission.Get, IdentityAccessKeyPermission.Decrypt, IdentityAccessKeyPermission.Encrypt, IdentityAccessKeyPermission.Update }
                    };
                }
                
                var patchProps = new KeyVaultPatchProperties();
                var policies = _keyVault.Data.Properties.AccessPolicies;
                foreach (var userObjectId in _userObjectIds)
                {
                    var existingPolicy = policies.FirstOrDefault(p => p.ObjectId == userObjectId);

                    if (existingPolicy != null) policies.Remove(existingPolicy);

                    var ap = new KeyVaultAccessPolicy(_keyVault.Data.Properties.TenantId, userObjectId, permissions);
                    policies.Add(ap);
                }

                foreach (var policy in policies)
                {
                    patchProps.AccessPolicies.Add(policy);
                }
            
                var patch = new KeyVaultPatch
                {
                    Properties = patchProps
                };
            
                var patchResponse = await _keyVault.UpdateAsync(patch);
                if (patchResponse.HasValue)
                {
                    ctx.SetState(ActionState.Success);
                }
                else
                {
                    ctx.SetErrorMessage("Unable to add users to Key Vault");
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