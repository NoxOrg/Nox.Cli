using System.Data;
using Microsoft.Data.SqlClient;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.MsSql;

public class MsSqlConnect_v1 : INoxCliAddin
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
                    Description = "The connection to the MsSql database",
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
    private SqlConnection? _connection;

    public Task BeginAsync(IDictionary<string, object> inputs)
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

        if (string.IsNullOrEmpty(_userId) ||
            string.IsNullOrEmpty(_password) ||
            string.IsNullOrEmpty(_database))
        {
            ctx.SetErrorMessage("The mssql connect action was not initialized");
            return outputs;
        }

        var csb = new SqlConnectionStringBuilder(_options)
        {
            DataSource = $"{_server},{_port}",
            UserID = _userId,
            Password = _password,
            InitialCatalog = _database,

        };

        _connection = new SqlConnection(csb.ToString());


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

