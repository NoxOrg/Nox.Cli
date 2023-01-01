using Nox.Cli.Actions;
using Npgsql;

namespace Nox.Cli.Plugins.Postgres;

public class PostgresSanitizeSqlString_v1 : NoxAction
{
    public override NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "postgres/sanitize-sql-string@v1",
            Author = "Andre Sharpe",
            Description = "Removes all non-word characters from a string",

            Inputs =
            {
                ["input-string"] = new NoxActionInput { 
                    Id = "input-string", 
                    Description = "The string to sanitize",
                    Default = string.Empty,
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["result"] = new NoxActionOutput {
                    Id = "result",
                    Description = "The sanitized string",
                },
            }
        };
    }

    private string _inputString = string.Empty;

    public override Task BeginAsync(NoxWorkflowExecutionContext ctx, IDictionary<string,object> inputs)
    {
        _inputString = (string)inputs["input-string"];

        return Task.FromResult(true);
    }

    public override Task<IDictionary<string, object>> ProcessAsync(NoxWorkflowExecutionContext ctx)
    {
        var outputs = new Dictionary<string, object?>();

        _state = ActionState.Error;

        outputs["result"] = _inputString.Sanitize();

        _state = ActionState.Success;

        return Task.FromResult((IDictionary<string,object>)outputs);
    }

    public override Task EndAsync(NoxWorkflowExecutionContext ctx)
    {
        return Task.FromResult(true);
    }
}

