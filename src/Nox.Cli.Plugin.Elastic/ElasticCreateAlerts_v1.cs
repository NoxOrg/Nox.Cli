using Nox.Cli.Abstractions;

namespace Nox.Cli.Plugin.Elastic;

public class ElasticCreateAlerts_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "elastic/create-alerts@v1",
            Author = "Jan Schutte",
            Description = "Create Elastic APM alerts for a microservice",

            Inputs =
            {
                ["service-name"] = new NoxActionInput
                {
                    Id = "service-name",
                    Description = "The name of your microservice",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["environment"] = new NoxActionInput
                {
                    Id = "environment",
                    Description = "The environment in which to create alerts. One of dev/test/production",
                    Default = "test",
                    IsRequired = true
                },

                ["support-email-address"] = new NoxActionInput
                {
                    Id = "support-email-address",
                    Description = "The email address where alerts will be sent",
                    Default = string.Empty,
                    IsRequired = true
                },
            }
        };
    }

    private string? _serviceName;
    private string? _environment;
    private string? _supportEmailAddress;

    public async Task BeginAsync(IDictionary<string, object> inputs)
    {
        throw new NotImplementedException();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        throw new NotImplementedException();
    }

    public async Task EndAsync()
    {
        throw new NotImplementedException();
    }
}