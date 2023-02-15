using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.Variables;
using Npgsql;

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

    private string? _inputString;

    public Task BeginAsync(IDictionary<string, IVariable> inputs)
    {
        _inputString = inputs.Value<string>("input-string");
        return Task.FromResult(true);
    }

    public  Task<IDictionary<string, IVariable>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, IVariable?>();

        ctx.SetState(ActionState.Error);
        if (string.IsNullOrEmpty(_inputString))
        {
            outputs["result"] = new Variable("");
        }
        else
        {
            outputs["result"] = new Variable( _inputString.Sanitize());    
        }
        

        ctx.SetState(ActionState.Success);

        return Task.FromResult((IDictionary<string, IVariable>)outputs);
    }

    public Task EndAsync()
    {
        return Task.FromResult(true);
    }
}

