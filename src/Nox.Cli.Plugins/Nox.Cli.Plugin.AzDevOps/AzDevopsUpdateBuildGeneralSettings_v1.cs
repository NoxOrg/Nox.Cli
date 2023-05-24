using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.AzDevOps;

public class AzDevopsUpdateBuildGeneralSettings_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "azdevops/update-build-general-settings@v1",
            Author = "Jan Schutte",
            Description = "Update the general settings of an Azure Devops project",

            Inputs =
            {
                ["connection"] = new NoxActionInput {
                    Id = "connection",
                    Description = "The connection established with action 'azdevops/connect@v1'",
                    Default = new VssConnection(new Uri("https://localhost"), null),
                    IsRequired = true
                },
                ["project-id"] = new NoxActionInput { 
                    Id = "project-id", 
                    Description = "The DevOps project Identifier",
                    Default = Guid.Empty,
                    IsRequired = true
                },
                ["enforce-referenced-repo-scoped-token"] = new NoxActionInput
                {
                    Id = "enforce-referenced-repo-scoped-token", 
                    Description = "Restricts the scope of access for all pipelines to only repositories explicitly referenced by the pipeline.",
                    Default = false,
                    IsRequired = false
                },
                ["status-badges-are-private"] = new NoxActionInput
                {
                    Id = "status-badges-are-private", 
                    Description = "Anonymous users can access the status badge API for all pipelines unless this option is enabled.",
                    Default = false,
                    IsRequired = false
                },
                ["enforce-settable-var"] = new NoxActionInput
                {
                    Id = "enforce-settable-var", 
                    Description = "If enabled, only those variables that are explicitly marked as \"Settable at queue time\" can be set at queue time.",
                    Default = false,
                    IsRequired = false
                },
                ["enforce-job-auth-scope"] = new NoxActionInput
                {
                    Id = "enforce-job-auth-scope", 
                    Description = "If enabled, scope of access for all pipelines reduces to the current project.",
                    Default = false,
                    IsRequired = false
                },
                ["publish-pipeline-metadata"] = new NoxActionInput
                {
                    Id = "publish-pipeline-metadata", 
                    Description = "Allows pipelines to record metadata.",
                    Default = false,
                    IsRequired = false
                }
            }
        };
    }

    private BuildHttpClient? _buildClient;
    private Guid? _projectId;
    private bool? _enforceReferencedRepoScopedToken;
    private bool? _statusBadgesArePrivate;
    private bool? _enforceSettableVar;
    private bool? _enforceJobAuthScope;
    private bool? _publishPipelineMetadata;
    private bool _isServerContext = false;

    public async Task BeginAsync(IDictionary<string,object> inputs)
    {
        var connection = inputs.Value<VssConnection>("connection");
        _projectId = inputs.Value<Guid>("project-id");
        _enforceReferencedRepoScopedToken = inputs.Value<bool?>("enforce-referenced-repo-scoped-token");
        _statusBadgesArePrivate = inputs.Value<bool?>("status-badges-are-private");
        _enforceSettableVar = inputs.Value<bool?>("enforce-settable-var");
        _enforceJobAuthScope = inputs.Value<bool?>("enforce-job-auth-scope");
        _publishPipelineMetadata = inputs.Value<bool?>("publish-pipeline-metadata");
        _buildClient = await connection!.GetClientAsync<BuildHttpClient>();
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        _isServerContext = ctx.IsServer;
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_buildClient == null ||
            _projectId == null ||
            _projectId == Guid.Empty)
        {
            ctx.SetErrorMessage("The devops find-build-definition action was not initialized");
        }
        else
        {
            try
            {
                var settings = await _buildClient.GetBuildGeneralSettingsAsync(_projectId.Value);
                if (_enforceReferencedRepoScopedToken.HasValue)
                {
                    settings.EnforceReferencedRepoScopedToken = _enforceReferencedRepoScopedToken.Value;
                }

                if (_statusBadgesArePrivate.HasValue)
                {
                    settings.StatusBadgesArePrivate = _statusBadgesArePrivate.Value;
                }

                if (_enforceSettableVar.HasValue)
                {
                    settings.EnforceSettableVar = _enforceSettableVar.Value;
                }

                if (_enforceJobAuthScope.HasValue)
                {
                    settings.EnforceJobAuthScope = _enforceJobAuthScope.Value;
                }

                if (_publishPipelineMetadata.HasValue)
                {
                    settings.PublishPipelineMetadata = _publishPipelineMetadata.Value;
                }
                
                await _buildClient.UpdateBuildGeneralSettingsAsync(settings, _projectId.Value);
                
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
        if (!_isServerContext && _buildClient != null) _buildClient.Dispose();
        return Task.CompletedTask;
    }
}