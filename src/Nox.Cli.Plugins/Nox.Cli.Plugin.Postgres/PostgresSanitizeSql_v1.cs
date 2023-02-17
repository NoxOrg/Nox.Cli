using Nox.Cli.Abstractions;

namespace Nox.Cli.Plugins.Postgres;

public class PostgresSanitizeSqlString_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
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

    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _inputString = (string)inputs["input-string"];

        return Task.FromResult(true);
    }

    public  Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object?>();

        ctx.SetState(ActionState.Error);

        outputs["result"] = _inputString.Sanitize();

        ctx.SetState(ActionState.Success);

        return Task.FromResult((IDictionary<string,object>)outputs);
    }

    public Task EndAsync()
    {
        return Task.FromResult(true);
    }
}

