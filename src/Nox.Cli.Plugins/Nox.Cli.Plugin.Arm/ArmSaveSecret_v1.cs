using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.Arm;

public class ArmSaveSecret_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "arm/save-secret@v1",
            Author = "Jan Schutte",
            Description = "Save a secret to an Azure key vault",

            Inputs =
            {
                ["key-vault-name"] = new NoxActionInput {
                    Id = "key-vault-name",
                    Description = "The name of the key vault to find",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["secret-name"] = new NoxActionInput {
                    Id = "secret-name",
                    Description = "The name of the secret to save",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["secret-value"] = new NoxActionInput {
                    Id = "secret-value",
                    Description = "The value of the secret to save",
                    Default = string.Empty,
                    IsRequired = true
                }
            }
        };
    }

    private string? _kvName;
    private string? _secretName;
    private string? _secretValue;
    private bool _isServerContext = false;

    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _kvName = inputs.Value<string>("key-vault-name");
        _secretName = inputs.Value<string>("secret-name");
        _secretValue = inputs.Value<string>("secret-value");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);
        
        var client = new SecretClient(new Uri($"https://{_kvName}.vault.azure.net"), new DefaultAzureCredential());

        if (client == null || 
            string.IsNullOrEmpty(_kvName))
        {
            ctx.SetErrorMessage("The arm save-secret action was not initialized");
        }
        else
        {
            try
            {
                var secretResponse = await client.SetSecretAsync(_secretName, _secretValue);
                if (secretResponse.HasValue)
                {
                    ctx.SetState(ActionState.Success);    
                }
                else
                {
                    ctx.SetErrorMessage("Saving the secret was not successful");
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