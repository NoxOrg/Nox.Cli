using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.Plugin.AzDevOps.Clients;
using Nox.Cli.Plugin.AzDevOps.Enums;
namespace Nox.Cli.Plugin.AzDevOps;

public class AzDevOpsVerifyAadGroup_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/verify-aad-group@v1",
            Author = "Jan Schutte",
            Description = "Verify that an AAD group ia available to a DevOps project group",
            Inputs =
            {
                ["server"] = new NoxActionInput { 
                    Id = "server", 
                    Description = "The DevOps server hostname or IP",
                    Default = "localhost",
                    IsRequired = true
                },
                
                ["personal-access-token"] = new NoxActionInput {
                    Id = "personal-access-token",
                    Description = "The personal access token to connect to DevOps with",
                    Default = string.Empty,
                    IsRequired = true
                },
                ["project-id"] = new NoxActionInput
                {
                    Id = "project-id",
                    Description = "The DevOps project Id",
                    Default = Guid.Empty,
                    IsRequired = true
                },
                ["aad-group-name"] = new NoxActionInput
                {
                    Id = "aad-group-name",
                    Description = "The AAD group to verify",
                    Default = Guid.Empty,
                    IsRequired = true
                }
            },
            Outputs =
            {
                ["is-found"] = new NoxActionOutput {
                    Id = "is-found",
                    Description = "A boolean indicating if the AAD group was found.",
                }
            }
        };
    }
    private string? _server;
    private string? _pat;
    private Guid? _projectId;
    private string? _aadGroupName;
    private bool _isServerContext = false;
    
    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _server = inputs.Value<string>("server");
        _pat = inputs.Value<string>("personal-access-token");
        _projectId = inputs.Value<Guid>("project-id");
        _aadGroupName = inputs.Value<string>("aad-group-name");
        return Task.CompletedTask;
    }
    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();
        ctx.SetState(ActionState.Error);
        
        if (string.IsNullOrWhiteSpace(_server) ||
            string.IsNullOrWhiteSpace(_pat) ||
            _projectId == null ||
            _projectId == Guid.Empty ||
            string.IsNullOrEmpty(_aadGroupName))
        {
            ctx.SetErrorMessage("The devops verify-aad-group action was not initialized");
        }
        else
        {
            try
            {
                var result = await FindGroup(ctx);
                outputs["is-found"] = result;
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
    private async Task<bool> FindGroup(INoxWorkflowContext ctx)
    {
        var identityPickerClient = new IdentityPickerClient(_server!, _pat!);
        var aadGroups = await identityPickerClient.FindIdentity(_aadGroupName!, IdentityType.Group);
        if (aadGroups == null || aadGroups.Count == 0)
        {
            return false;
        }
        return true;
    }
    
}