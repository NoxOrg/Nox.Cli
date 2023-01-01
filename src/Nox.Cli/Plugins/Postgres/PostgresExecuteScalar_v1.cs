using Nox.Cli.Actions;
using Npgsql;

namespace Nox.Cli.Plugins.Postgres;

public class PostgresExecuteScalar_v1 : NoxAction
{
    public override NoxActionMetaData Discover()
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

    public override Task BeginAsync(NoxWorkflowExecutionContext ctx, IDictionary<string,object> inputs)
    {
        _connection = (NpgsqlConnection)inputs["connection"];

        _sql = (string)inputs["sql"];

        if (inputs.ContainsKey("parameters"))
        {
            _parameters = (List<object>)inputs["parameters"];
        }

        return Task.FromResult(true);
    }

    public override async Task<IDictionary<string, object>> ProcessAsync(NoxWorkflowExecutionContext ctx)
    {
        var outputs = new Dictionary<string, object?>();

        _state = ActionState.Error;

        if (_connection == null)
        {
            _errorMessage = "The Postgres connect action was not initialized";
        }
        else if (_sql == null)
        {
            _errorMessage = "The sql query was not initialized";
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

                _state = ActionState.Success;
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
            }
        }

        return outputs!;
    }

    public override Task EndAsync(NoxWorkflowExecutionContext ctx)
    {
        return Task.FromResult(true);
    }
}

