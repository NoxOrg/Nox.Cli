using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Core.Configuration;

namespace Nox.Cli.Plugin.Project;

public class ProjectGetTeamEmails_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "project/get-team-emails@v1",
            Author = "Jan Schutte",
            Description = "Get a list of team member email addresses from the Nox Project Definition",

            Inputs =
            {
                ["team-members"] = new NoxActionInput {
                    Id = "team-members",
                    Description = "The list of developers on the project",
                    Default = new List<TeamMemberConfiguration>(),
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
                ["team-emails"] = new NoxActionOutput
                {
                    Id = "team-emails",
                    Description = "The resulting concatenated string of team member email addresses."
                },
            }
        };
    }

    private List<TeamMemberConfiguration>? _members;
    private bool? _includeAdmin;
    private string? _delimiter;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _members = inputs.Value<List<TeamMemberConfiguration>>("team-members");
        _includeAdmin = inputs.ValueOrDefault<bool>("include-admin", this);
        _delimiter = inputs.ValueOrDefault<string>("delimiter", this);
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_members == null || 
            _members.Count == 0 ||
            _includeAdmin == null ||
            string.IsNullOrEmpty(_delimiter))
        {
            ctx.SetErrorMessage("The Project get-team-emails action was not initialized");
        }
        else
        {
            try
            {
                var result = "";
                foreach (var item in _members)
                {
                    if (item.IsAdmin && !_includeAdmin == true) continue;
                    if (!string.IsNullOrEmpty(item.Email))
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

                outputs["team-emails"] = result!;
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