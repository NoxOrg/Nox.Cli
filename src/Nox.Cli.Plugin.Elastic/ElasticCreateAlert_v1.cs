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
                
                ["alert-name"] = new NoxActionInput
                {
                    Id = "alert-name",
                    Description = "The name of the alert to add, this must exist in your manifest",
                    Default = string.Empty,
                    IsRequired = false
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

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
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
        
        //Get the 

        if (_aadClient == null || string.IsNullOrEmpty(_groupName))
        {
            ctx.SetErrorMessage("The az active directory create-group action was not initialized");
        }
        else
        {
            if (string.IsNullOrEmpty(_groupDescription)) _groupDescription = _groupName;
            try
            {
                var projectGroupName = _groupName.ToUpper();

                var projectGroups = await _aadClient.Groups.Request()
                    .Filter($"DisplayName eq '{projectGroupName}'")
                    .Expand("Members")
                    .GetAsync();

                if (projectGroups.Count == 1)
                {
                    outputs["aad-group"] = projectGroups.First();
                }
                else
                {
                    outputs["aad-group"] = await CreateAdGroupAsync(projectGroupName);
                }
                ctx.SetState(ActionState.Success);
            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage(ex.Message);
            }
        }

        return outputs;
    }

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }
}