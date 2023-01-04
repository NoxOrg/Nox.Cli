﻿using Nox.Cli.Actions;
using Npgsql;
using System.Data;

namespace Nox.Cli.Plugins.Postgres;

public class PostgresConnect_v1 : NoxAction
{
    public override NoxActionMetaData Discover()
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

    private NpgsqlConnection? _connection;

    public override Task BeginAsync(NoxWorkflowExecutionContext ctx, IDictionary<string,object> inputs)
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = (string)inputs["server"],
            Port = Convert.ToInt32(inputs["port"]),
            Username = (string)inputs["user"],
            Password = (string)inputs["password"],
            Database = (string)inputs["database"],
        };

        _connection = new NpgsqlConnection(csb.ToString());

        return Task.FromResult(true);
    }

    public override async Task<IDictionary<string, object>> ProcessAsync(NoxWorkflowExecutionContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        _state = ActionState.Error;

        if (_connection == null)
        {
            _errorMessage = "The Postgres connect action was not initialized";
        }
        else
        {
            if (_connection.State == ConnectionState.Closed || _connection.State == ConnectionState.Broken)
            {
                try
                {
                    await _connection.OpenAsync();
 
                    outputs["connection"] = _connection;

                    _state = ActionState.Success;
                }
                catch (Exception ex)
                {
                    _errorMessage = ex.Message;
                }
            }
        }

        return outputs;
    }

    public override async Task EndAsync(NoxWorkflowExecutionContext ctx)

    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }
}
