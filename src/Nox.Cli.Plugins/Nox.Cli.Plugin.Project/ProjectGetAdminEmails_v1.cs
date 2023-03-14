using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Core.Configuration;

namespace Nox.Cli.Plugin.Project;

public class ProjectGetAdminEmails_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "project/get-admin-emails@v1",
            Author = "Jan Schutte",
            Description = "Get a list of admin team member email addresses from the Nox Project Definition",

            Inputs =
            {
                ["project-config"] = new NoxActionInput {
                    Id = "project-config",
                    Description = "The Nox project configuration",
                    Default = new ProjectConfiguration(),
                    IsRequired = true
                },
                
                ["delimiter"] = new NoxActionInput {
                    Id = "delimiter",
                    Description = "The delimiter to use in the concatenated result string",
                    Default = ",",
                    IsRequired = true
                }
            },

            Outputs =
            {
                ["result"] = new NoxActionOutput
                {
                    Id = "result",
                    Description = "The resulting concatenated string of admin team member email addresses."
                },
            }
        };
    }

    private ProjectConfiguration? _config;
    private string? _delimiter;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _config = inputs.Value<ProjectConfiguration>("project-config");
        _delimiter = inputs.ValueOrDefault<string>("delimiter", this);
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_config == null || 
            string.IsNullOrEmpty(_delimiter))
        {
            ctx.SetErrorMessage("The Project get-admin-emails action was not initialized");
        }
        else
        {
            try
            {
                var result = "";
                foreach (var item in _config.Team.Developers)
                {
                    if (!string.IsNullOrEmpty(item.Email) && item.IsAdmin)
                    {
                        if (string.IsNullOrEmpty(result))
                        {
                            result = item.Email;
                        }
                        else
                        {
                            result += _delimiter + item.Email;
                        }
                    }
                }

                outputs["result"] = result!;
                ctx.SetState(ActionState.Success);
            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage(ex.Message);
            }
        }
        
        return Task.FromResult<IDictionary<string, object>>(outputs);
    }

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }
}