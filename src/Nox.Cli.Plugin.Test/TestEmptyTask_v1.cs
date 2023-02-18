using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.Test;

public class TestEmptyTask_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "test/empty-task@v1",
            Author = "Jan Schutte",
            Description = "Does nothing",

            Inputs =
            {
                ["my-variable"] = new NoxActionInput {
                    Id = "my-variable",
                    Description = "An arbitrary variable",
                    Default = string.Empty,
                    IsRequired = false
                }
            },
            
            Outputs =
            {
                ["my-result"] = new NoxActionOutput {
                    Id = "my-result",
                    Description = "An arbitrary result",
                },
            }
        };
    }

    private string? _myVar;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _myVar = inputs.Value<string>("my-variable");
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>
        {
            ["my-result"] = _myVar!
        };
        return Task.FromResult((IDictionary<string,object>)outputs);
    }

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }
}