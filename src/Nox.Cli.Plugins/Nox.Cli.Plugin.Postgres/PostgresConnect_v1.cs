using System.Data;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Npgsql;

namespace Nox.Cli.Plugin.Postgres;

public class PostgresConnect_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "postgres/connect@v1",
            Author = "Andre Sharpe",
            Description = "Connect to and execute queries on Postgres",

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
                    Default = 5432,
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
                ["options"] = new NoxActionInput {
                    Id = "options",
                    Description = "The database options to use when connecting to the database",
                    Default = string.Empty,
                    IsRequired = false
                }
            },

            Outputs =
            {
                ["connection"] = new NoxActionOutput {
                    Id = "connection",
                    Description = "The database hostname or IP",
                },
            }
        };
    }

    private bool _isServerContext = false;
    private string? _server;
    private string? _options;
    private int? _port;
    private string? _userId;
    private string? _password;
    private string? _database;
    private NpgsqlConnection? _connection;
    

    public Task BeginAsync(IDictionary<string,object> inputs)
    {
        _server = inputs.ValueOrDefault<string>("server", this);
        _port = inputs.ValueOrDefault<int>("port", this);
        _userId = inputs.Value<string>("user");
        _password = inputs.Value<string>("password");
        _options = inputs.ValueOrDefault<string>("options", this);
        _database = inputs.Value<string>("database");
        
        return Task.FromResult(true);
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);
        
        var csb = new NpgsqlConnectionStringBuilder(_options)
        {
            Host = _server,
            Port = _port!.Value,
            Username = _userId,
            Password = _password,
            Database = _database,
        };

        _connection = new NpgsqlConnection(csb.ToString());


        if (_connection == null)
        {
            ctx.SetErrorMessage("The Postgres connect action was not initialized");
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
        if (!_isServerContext)
        {
            await _connection!.CloseAsync();
            await _connection.DisposeAsync();
        }
    }
}

