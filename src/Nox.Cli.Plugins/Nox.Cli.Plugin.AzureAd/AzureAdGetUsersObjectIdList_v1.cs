using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugins.AzDevops;

public class AzureAdGetUsersObjectIdList_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azuread/get-users-object-id-list@v1",
            Author = "Jan Schutte",
            Description = "Get the Object Ids of a list of Azure user entities",

            Inputs =
            {
                ["aad-client"] = new NoxActionInput
                {
                    Id = "aad-client",
                    Description = "The AAD client",
                    Default = new GraphServiceClient(new HttpClient()),
                    IsRequired = true
                },
                
                ["user-names"] = new NoxActionInput
                {
                    Id = "user-names",
                    Description = "a comma separated list of AAD usernames of the users to find",
                    Default = string.Empty,
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
                ["object-ids"] = new NoxActionOutput
                {
                    Id = "object-ids",
                    Description = "a Delimited string containing the resolved AAD Object Ids",
                },
            }

        };
    }

    private GraphServiceClient? _aadClient;
    private string? _userNames;
    private string? _delimiter;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _aadClient = inputs.Value<GraphServiceClient>("aad-client");
        _userNames = inputs.Value<string>("user-names");
        _delimiter = inputs.ValueOrDefault<string>("delimiter", this);
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_aadClient == null || 
            string.IsNullOrEmpty(_userNames))
        {
            ctx.SetErrorMessage("The az active directory get-users-object-id-list action was not initialized");
        }
        else
        {
            try
            {
                var result = "";

                var userNameList = _userNames.Split(_delimiter);
                foreach (var username in userNameList)
                {
                    var users = await _aadClient.Users.GetAsync((requestConfiguration) =>
                    {
                        requestConfiguration.QueryParameters.Count = true;
                        requestConfiguration.QueryParameters.Filter = $"UserPrincipalName eq '{username}'";
                    });

                    if (users != null && users.Value!.Count != 1) continue;
                    var user = users!.Value!.First();
                    if (string.IsNullOrEmpty(result))
                    {
                        result = user.Id;
                    }
                    else
                    {
                        result += _delimiter + user.Id;
                    }
                }

                outputs["object-ids"] = result;
                
                ctx.SetState(ActionState.Success);
            }
            catch (ODataError odataError)
            {
                ctx.SetErrorMessage(odataError.Error!.Message!);
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