using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using ActionState = Nox.Cli.Abstractions.ActionState;

namespace Nox.Cli.Plugins.AzDevops;

public class AzureAdGetUserObjectId_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azuread/get-user-object-id@v1",
            Author = "Jan Schutte",
            Description = "Get the Object Id of an Azure user entity",

            Inputs =
            {
                ["aad-client"] = new NoxActionInput
                {
                    Id = "aad-client",
                    Description = "The AAD client",
                    Default = new GraphServiceClient(new HttpClient()),
                    IsRequired = true
                },
                
                ["user-name"] = new NoxActionInput
                {
                    Id = "user-name",
                    Description = "The AAD username of the user to find",
                    Default = string.Empty,
                    IsRequired = true
                }
            },
            
            Outputs =
            {
                ["object-id"] = new NoxActionOutput
                {
                    Id = "object-id",
                    Description = "The Object Id of the AAD user",
                },
            }

        };
    }

    private GraphServiceClient? _aadClient;
    private string? _username;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _aadClient = inputs.Value<GraphServiceClient>("aad-client");
        _username = inputs.Value<string>("user-name");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_aadClient == null || 
            string.IsNullOrEmpty(_username))
        {
            ctx.SetErrorMessage("The az active directory get-user-object-id action was not initialized");
        }
        else
        {
            try
            {
                var users = await _aadClient.Users.GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Count = true;
                    requestConfiguration.QueryParameters.Filter = $"UserPrincipalName eq '{_username}'";
                });
                
                if (users != null && users.Value!.Count == 1)
                {
                    var user = users.Value.First();
                    outputs["object-id"] = user.Id!;
                }
                else
                {
                    ctx.SetErrorMessage($"AAD User {_username} not found.");
                }
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