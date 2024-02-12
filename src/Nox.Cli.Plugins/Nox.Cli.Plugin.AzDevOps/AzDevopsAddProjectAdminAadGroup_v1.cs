using System.Text.Json;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.Abstractions.Helpers;
using Nox.Cli.Plugin.AzDevOps.Clients;
using Nox.Cli.Plugin.AzDevOps.DTO;
using Nox.Cli.Plugin.AzDevOps.Enums;
using Nox.Cli.Plugin.AzDevOps.Helpers;
using RestSharp;
namespace Nox.Cli.Plugin.AzDevOps;

public class AzDevopsAddProjectAdminAadGroup_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/add-project-admin-aad-group@v1",
            Author = "Jan Schutte",
            Description = "Add an AAD group as administrators on a DevOps project",

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
                    Description = "The AAD group to add",
                    Default = Guid.Empty,
                    IsRequired = true
                }
            }
        };
    }

    private string? _server;
    private string? _pat;
    private Guid? _projectId;
    private string? _groupName;
    private bool _isServerContext = false;
    
    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _server = inputs.Value<string>("server");
        _pat = inputs.Value<string>("personal-access-token");
        _projectId = inputs.Value<Guid>("project-id");
        _groupName = inputs.Value<string>("aad-group-name");
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
            string.IsNullOrEmpty(_groupName))
        {
            ctx.SetErrorMessage("The devops add-project-admin-aad-group action was not initialized");
        }
        else
        {
            try
            {
                var result = await AddAdmins(ctx);
                if (result)
                {
                    ctx.SetState(ActionState.Success);
                }
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

    private async Task<bool> AddAdmins(INoxWorkflowContext ctx)
    {
        var identityPickerClient = new IdentityPickerClient(_server!, _pat!);
        var graphClient = new GraphClient(_server!, _pat!);

        var aadGroups = await identityPickerClient.FindIdentity(_groupName!, IdentityType.Group);
        if (aadGroups == null)
        {
            ctx.SetErrorMessage($"Unable to locate the AAD group: {_groupName}");
            return false;
        }

        var aadGroup = aadGroups.First();
        

        var projectDescriptor = await graphClient.GetDescriptor(_projectId.ToString()!);
        var projectGroup = await graphClient.FindProjectGroup(projectDescriptor!, "\\Project Administrators");
        if (projectGroup == null)
        {
            ctx.SetErrorMessage($"Unable to locate the project administrator group for this DevOps project");
            return false;
        }

        var aadGroupDescriptor = await graphClient.GetDescriptor(projectGroup.Descriptor!, aadGroup.OriginId!);

        if (string.IsNullOrEmpty(aadGroupDescriptor))
        {
            ctx.SetErrorMessage($"Unable to retrieve AAD group descriptor.");
            return false;
        }
        
        return await graphClient.AddGroupMembership(projectGroup.Descriptor!, aadGroupDescriptor!);


    }

    
}