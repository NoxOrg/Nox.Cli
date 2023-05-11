using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.Elastic;

public class ElasticCreateAlert_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "elastic/create-alert@v1",
            Author = "Jan Schutte",
            Description = "Create an Elastic APM alert for a microservice",

            Inputs =
            {
                ["project-name"] = new NoxActionInput
                {
                    Id = "project-name",
                    Description = "The name of your project",
                    Default = string.Empty,
                    IsRequired = false
                },
                
                ["alert-body"] = new NoxActionInput
                {
                    Id = "alert-body",
                    Description = "The body of the alert to add",
                    Default = string.Empty,
                    IsRequired = false
                }
            }
        };
    }

    private string? _projectName;
    private string? _alertName;
    private string? _environment;
    private string? _supportEmailAddress;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _projectName = inputs.ValueOrDefault<string>("project-name", this);
        _alertName = inputs.ValueOrDefault<string>("alert-name", this);
        _environment = inputs.ValueOrDefault<string>("environment", this);
        _supportEmailAddress = inputs.ValueOrDefault<string>("support-email-address", this);
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_projectName) ||
            string.IsNullOrEmpty(_alertName) ||
            string.IsNullOrEmpty(_environment) ||
            string.IsNullOrEmpty(_supportEmailAddress))
        {
            ctx.SetErrorMessage("The elastic create-alert action was not initialized");
        }

        return Task.FromResult<IDictionary<string, object>>(outputs);
    }

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }
}