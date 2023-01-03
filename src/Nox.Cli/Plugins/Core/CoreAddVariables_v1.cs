using Nox.Cli.Actions;

namespace Nox.Cli.Plugins.Core;

public class CoreAddVariables_v1 : NoxAction
{
    public override NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "core/add-variables@v1",
            Author = "Andre Sharpe",
            Description = "Adds variables to the workflow",

            Inputs =
            {
                ["<variable1>"] = new NoxActionInput {
                    Id = "<variable1>",
                    Description = "The name of the variable",
                    Default = string.Empty,
                    IsRequired = false
                },
                ["<variable2>"] = new NoxActionInput {
                    Id = "<variable2>",
                    Description = "The name of the variable",
                    Default = string.Empty,
                    IsRequired = false
                },
                ["<variable...>"] = new NoxActionInput {
                    Id = "<variable...>",
                    Description = "The name of the variable",
                    Default = string.Empty,
                    IsRequired = false
                },
            },
        };
    }

    private IDictionary<string,object> _variables=null!;

    public override Task BeginAsync(NoxWorkflowExecutionContext ctx, IDictionary<string,object> inputs)
    {
        _variables = inputs;
        
        return Task.FromResult(true);
    }

    public override Task<IDictionary<string,object>> ProcessAsync(NoxWorkflowExecutionContext ctx)
    {
        var outputs = new Dictionary<string,object>();

        foreach (var (key, value) in _variables)
        {
            ctx.AddToVariables(key, value);
        }

        _state = ActionState.Success;

        return Task.FromResult((IDictionary<string,object>)outputs);
    }

    public override Task EndAsync(NoxWorkflowExecutionContext ctx)

    {
        return Task.FromResult(true);
    }
}

