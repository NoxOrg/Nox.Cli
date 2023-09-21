using Microsoft.Data.SqlClient;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.MsSql;

public class MsSqlExecuteNonQuery_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "mssql/execute-nonquery@v1",
            Author = "Jan Schutte",
            Description = "Execute a non-query statement on Ms Sql Server",

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
                    Default = new SqlConnection(),
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
                    Description = "The integer result of the non-query",
                },
            }
        };
    }

    private bool _isServerContext = false;
    private SqlConnection? _connection;

    private string? _sql;

    private List<object>? _parameters;

    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _connection = inputs.Value<SqlConnection>("connection");
        _sql = inputs.Value<string>("sql");
        _parameters = inputs.Value<List<object>>("parameters");
        return Task.FromResult(true);
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_connection == null ||
            string.IsNullOrEmpty(_sql))
        {
            ctx.SetErrorMessage("The mssql execute scalar action was not initialized");
            return outputs;
        }

        try
        {
            await using var cmd = new SqlCommand(_sql, _connection);

            if (_parameters != null)
            {
                foreach (var p in _parameters)
                {
                    cmd.Parameters.Add(new SqlParameter() { Value = p });
                }
            }

            var result = await cmd.ExecuteNonQueryAsync();

            outputs["result"] = result;

            ctx.SetState(ActionState.Success);
        }
        catch (Exception ex)
        {
            ctx.SetErrorMessage(ex.Message);
        }

        return outputs!;
    }

    public Task EndAsync()
    {
        return Task.FromResult(true);
    }
}
