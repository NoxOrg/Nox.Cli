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
                ["team-members"] = new NoxActionInput {
                    Id = "team-members",
                    Description = "The list of developers on the project",
                    Default = new List<TeamMemberConfiguration>(),
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
                ["team-admin-emails"] = new NoxActionOutput
                {
                    Id = "team-admin-emails",
                    Description = "The resulting concatenated string of admin team member email addresses."
                },
            }
        };
    }

    private List<TeamMemberConfiguration>? _members;
    private string? _delimiter;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _members = inputs.Value<List<TeamMemberConfiguration>>("team-members");
        _delimiter = inputs.ValueOrDefault<string>("delimiter", this);
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_members == null || 
            _members.Count == 0 || 
            string.IsNullOrEmpty(_delimiter))
        {
            ctx.SetErrorMessage("The Project get-admin-emails action was not initialized");
        }
        else
        {
            try
            {
                var result = "";
                foreach (var item in _members)
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

                outputs["team-admin-emails"] = result!;
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