using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.Arm;

public class ArmGetSecret_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "arm/get-secret@v1",
            Author = "Jan Schutte",
            Description = "Get a secret from an Azure key vault",

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
                }
            },
            Outputs =
            {
                ["secret-value"] = new NoxActionOutput {
                    Id = "secret-value",
                    Description = "The value of the secret in the key vault.",
                },
            }
        };
    }

    private string? _kvName;
    private string? _secretName;
    private bool _isServerContext = false;

    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _kvName = inputs.Value<string>("key-vault-name");
        _secretName = inputs.Value<string>("secret-name");
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
                var secretResponse = await client.GetSecretAsync(_secretName);
                if (secretResponse.HasValue)
                {
                    outputs["secret-value"] = secretResponse.Value.Value;
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