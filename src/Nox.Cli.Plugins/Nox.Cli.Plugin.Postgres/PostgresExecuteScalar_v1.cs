using Nox.Cli.Abstractions;
using Npgsql;

namespace Nox.Cli.Plugins.Postgres;

public class PostgresExecuteScalar_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "postgres/execute-scalar@v1",
            Author = "Andre Sharpe",
            Description = "Execute a scalar query on Postgres",

            Inputs =
            {
                ["sql"] = new NoxActionInput { 
                    Id = "sql", 
                    Description = "The sql query to execute",
                    Default = "SELECT 1",
                    IsRequired = true
                },

                ["connection"] = new NoxActionInput {
                    Id = "connection",
                    Description = "The connection established with action 'postgres/connect@v1'",
                    Default = new NpgsqlConnection(),
                    IsRequired = true
                },
                ["parameters"] = new NoxActionInput {
                    Id = "parameters",
                    Description = "The parameters for the query",
                    Default = new object[] {},
                    IsRequired = false
                },
            },

            Outputs =
            {
                ["result"] = new NoxActionOutput {
                    Id = "result",
                    Description = "The result of the scalar query",
                },
            }
        };
    }

    private NpgsqlConnection? _connection;

    private string? _sql;

    private List<object>? _parameters;

    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _connection = (NpgsqlConnection)inputs["connection"];

        _sql = (string)inputs["sql"];

        if (inputs.ContainsKey("parameters"))
        {
            _parameters = (List<object>)inputs["parameters"];
        }

        return Task.FromResult(true);
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object?>();

        ctx.SetState(ActionState.Error);

        if (_connection == null)
        {
            ctx.SetErrorMessage("The Postgres connect action was not initialized");
        }
        else if (_sql == null)
        {
            ctx.SetErrorMessage("The sql query was not initialized");
        }
        else
        {
            try
            {
                using var cmd = new NpgsqlCommand(_sql, _connection);

                if (_parameters != null)
                {
                    foreach (var p in _parameters)
                    {
                        cmd.Parameters.Add( new NpgsqlParameter { Value = p } );
                    }
                }

                var result = await cmd.ExecuteScalarAsync();

                outputs["result"] = result ?? new object();

                ctx.SetState(ActionState.Success);
            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage(ex.Message);
            }
        }

        return outputs!;
    }

    public Task EndAsync(INoxWorkflowContext ctx)
    {
        return Task.CompletedTask;
    }
}

