using System.Data;
using Microsoft.Data.SqlClient;
using Nox.Cli.Abstractions;

namespace Nox.Cli.Plugin.MsSql;

public class PostgresConnect_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "mssql/connect@v1",
            Author = "Jan Schutte",
            Description = "Connect to an MsSql database",

            Inputs =
            {
                ["server"] = new NoxActionInput { 
                    Id = "server", 
                    Description = "The database hostname or IP",
                    Default = "localhost",
                    IsRequired = false
                },

                ["port"] = new NoxActionInput {
                    Id = "port",
                    Description = "The database port to connect via",
                    Default = 1433,
                    IsRequired = false
                },

                ["user"] = new NoxActionInput {
                    Id = "user",
                    Description = "The username to connect as",
                    Default = string.Empty,
                    IsRequired = true
                },

                ["password"] = new NoxActionInput {
                    Id = "password",
                    Description = "The password to connect with",
                    Default = string.Empty,
                    IsRequired = true
                },

                ["database"] = new NoxActionInput {
                    Id = "database",
                    Description = "The database name to connect to",
                    Default = string.Empty,
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["connection"] = new NoxActionOutput {
                    Id = "connection",
                    Description = "The connection to the MsSql database",
                },
            }
        };
    }

    private SqlConnection? _connection;

    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        var csb = new SqlConnectionStringBuilder
        {
            DataSource = $"{(string)inputs["server"]},{(string)inputs["port"]}",
            UserID = (string)inputs["user"],
            Password = (string)inputs["password"],
            InitialCatalog = (string)inputs["database"],
        };

        _connection = new SqlConnection(csb.ToString());

        return Task.FromResult(true);
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_connection == null)
        {
            ctx.SetErrorMessage("The mssql connect action was not initialized");
        }
        else
        {
            if (_connection.State == ConnectionState.Closed || _connection.State == ConnectionState.Broken)
            {
                try
                {
                    await _connection.OpenAsync();
 
                    outputs["connection"] = _connection;

                    ctx.SetState(ActionState.Success);
                }
                catch (Exception ex)
                {
                    ctx.SetErrorMessage(ex.Message);
                }
            }
        }

        return outputs;
    }

    public async Task EndAsync()

    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }
}

