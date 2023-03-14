using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Core.Configuration;

namespace Nox.Cli.Plugin.Project;

public class ProjectGetTeamUsernames_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "project/get-team-usernames@v1",
            Author = "Jan Schutte",
            Description = "Get a list of team member usernames from the Nox Project Definition",

            Inputs =
            {
                ["project-config"] = new NoxActionInput {
                    Id = "project-config",
                    Description = "The Nox project configuration",
                    Default = new ProjectConfiguration(),
                    IsRequired = true
                },
                
                ["include-admin"] = new NoxActionInput {
                    Id = "include-admin",
                    Description = "flag to indicate if admin team members must be included in the list",
                    Default = true,
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
                    Description = "The resulting concatenated string of team member usernames."
                },
            }
        };
    }

    private ProjectConfiguration? _config;
    private bool? _includeAdmin;
    private string? _delimiter;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _config = inputs.Value<ProjectConfiguration>("project-config");
        _includeAdmin = inputs.ValueOrDefault<bool>("include-admin", this);
        _delimiter = inputs.ValueOrDefault<string>("delimiter", this);
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_config == null || 
            _includeAdmin == null ||
            string.IsNullOrEmpty(_delimiter))
        {
            ctx.SetErrorMessage("The Project get-team-usernames action was not initialized");
        }
        else
        {
            try
            {
                var result = "";
                foreach (var item in _config.Team.Developers)
                {
                    if (!string.IsNullOrEmpty(item.UserName))
                    {
                        if (string.IsNullOrEmpty(result))
                        {
                            result = item.UserName;
                        }
                        else
                        {
                            result += _delimiter + item.UserName;
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