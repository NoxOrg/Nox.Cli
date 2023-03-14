using Azure.Identity;
using Azure.ResourceManager;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Core.Exceptions;

namespace Nox.Cli.Plugin.Arm;

public class ArmConnect_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "arm/connect@v1",
            Author = "Jan Schutte",
            Description = "Connect to an Azure Resource Management Subscription",

            Inputs =
            {
              ["subscription-id"] = new NoxActionInput
              {
                  Id = "subscription-id",
                  Description = "The Subscription Id of the Azure subscription to connect to",
                  Default = string.Empty,
                  IsRequired = true
              }  
            },
            
            Outputs =
            {
                ["subscription"] = new NoxActionOutput
                {
                    Id = "subscription",
                    Description = "The ARM subscription that was connected to using the default Azure credentials",
                },
            }
        };
    }

    private string? _subscriptionId;

    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _subscriptionId = inputs.Value<string>("subscription-id");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);
        
        if (string.IsNullOrEmpty(_subscriptionId))
        {
            ctx.SetErrorMessage("The arm connect action was not initialized");
        }
        else
        {
            try
            {
                var client = new ArmClient(new DefaultAzureCredential());
                var subs = client.GetSubscriptions();
                var subResponse = await subs.GetAsync(_subscriptionId);
                var sub = subResponse.Value;
                outputs["subscription"] = sub ?? throw new NoxException($"Unable to connect to subscription {_subscriptionId}");
                ctx.SetState(ActionState.Success);
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