using Microsoft.Graph;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugins.AzDevops;

public class AzureAdGetGroupMemberIds_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azuread/get-group-member-ids@v1",
            Author = "Jan Schutte",
            Description = "Get the user object ids of an an Azure Active Directory group",

            Inputs =
            {
                ["aad-client"] = new NoxActionInput
                {
                    Id = "aad-client",
                    Description = "The AAD client",
                    Default = new GraphServiceClient(new HttpClient()),
                    IsRequired = true
                },
                
                ["group-id"] = new NoxActionInput
                {
                    Id = "group-id",
                    Description = "The Id of the aad group from which to get the member ids",
                    Default = string.Empty,
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["member-ids"] = new NoxActionOutput
                {
                    Id = "member-ids",
                    Description = "a Delimited string containing the resolved AAD member Ids",
                },
            }
        };
    }

    private string? _groupId;
    private GraphServiceClient? _aadClient;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _groupId = inputs.Value<string>("group-id");
        _aadClient = inputs.Value<GraphServiceClient>("aad-client");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_aadClient == null || 
            string.IsNullOrEmpty(_groupId))
        {
            ctx.SetErrorMessage("The az active directory get-group-member-ids action was not initialized");
        }
        else
        {
            try
            {
                var members = await _aadClient.Groups[_groupId].Members.GetAsync(config =>
                {
                    config.QueryParameters.Count = true;
                    config.QueryParameters.Select = new[] { "id" };
                });

                var ids = "";

                if (members is { Value.Count: > 0 })
                {
                    foreach (var member in members.Value)
                    {
                        if (string.IsNullOrEmpty(ids))
                        {
                            ids = member.Id;
                        }
                        else
                        {
                            ids += "," + member.Id;
                        }
                    }
                }

                outputs["member-ids"] = ids;
                
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