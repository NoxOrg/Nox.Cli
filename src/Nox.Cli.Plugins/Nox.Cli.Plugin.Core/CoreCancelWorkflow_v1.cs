using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.Core;

public class CoreCancelWorkflow_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "core/cancel-workflow@v1",
            Author = "Jan Schutte",
            Description = "Cancels execution of a workflow, with a reason message.",

            Inputs =
            {
                ["reason"] = new NoxActionInput {
                    Id = "reason",
                    Description = "The reason for cancelling the workflow execution.",
                    Default = string.Empty,
                    IsRequired = true
                }
            },
        };
    }

    private string? _reason;

    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _reason = inputs.Value<string>("reason");
        
        return Task.FromResult(true);
    }

    public Task<IDictionary<string,object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string,object>();

        if (string.IsNullOrEmpty(_reason))
        {
            ctx.SetErrorMessage("The Core cancel-workflow action was not initialized");
        }
        else
        {
            ctx.RequestCancellation(_reason);
        }

        ctx.SetState( ActionState.Success );

        return Task.FromResult((IDictionary<string,object>)outputs);
    }

    public Task EndAsync()

    {
        return Task.FromResult(true);
    }
}