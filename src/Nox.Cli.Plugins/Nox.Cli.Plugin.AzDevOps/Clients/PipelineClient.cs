using System.Text.Json;
using Nox.Cli.Abstractions.Helpers;
using Nox.Cli.Plugin.AzDevOps.DTO;
using Nox.Cli.Plugin.AzDevOps.Exceptions;
using Nox.Cli.Plugin.AzDevOps.Helpers;
using RestSharp;

namespace Nox.Cli.Plugin.AzDevOps.Clients;

public class PipelineClient
{
    private readonly RestClient _client;
    private readonly string _pat;
        
    
    public PipelineClient(string serverUri, string pat)
    {
        _client = new RestClient(serverUri);
        _pat = pat;
    }

    public async Task AuthorizeAgentQueuePipelines(Guid projectId, int queueId)
    {
        var request = new RestRequest($"/{projectId}/_apis/pipelines/pipelinePermissions/queue/{queueId}")
        {
            Method = Method.Patch
        };
        AddHeaders(request);
        var payload = new AuthorizeRequest()
        {
            Resource = new Resource
            {
                Type = "queue",
                Id = queueId.ToString()
            },
            AllPipelines = new PipelineAuthorizeAll
            {
                Authorized = true
            }
        };
        request.AddJsonBody(JsonSerializer.Serialize(payload,  JsonOptions.Instance));
        var response = await _client.ExecuteAsync<AuthorizeResponse>(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new DevOpsClientException($"An error occurred while trying to authorize the pipelines on a project ({response.Content})");
        }
    }

    public async Task AuthorizeEndpointPipeline(Guid projectId, Guid endpointId, int pipelineId)
    {
        var request = new RestRequest($"/{projectId}/_apis/pipelines/pipelinePermissions/endpoint/{endpointId}")
        {
            Method = Method.Patch
        };
        AddHeaders(request);
        var payload = new AuthorizeRequest()
        {
            Pipelines = new List<PipelineAuthorize>
            {
                new PipelineAuthorize
                {
                    Id = pipelineId,
                    Authorized = true
                }
            }
        };
        request.AddJsonBody(JsonSerializer.Serialize(payload,  JsonOptions.Instance));
        var response = await _client.ExecuteAsync<AuthorizeResponse>(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new DevOpsClientException($"An error occurred while trying to authorize the pipelines on a project ({response.ErrorMessage})");
        }
    }

    public async Task AuthorizeEnvironmentPipeline(Guid projectId, int environmentId, int pipelineId)
    {
        var request = new RestRequest($"/{projectId}/_apis/pipelines/pipelinePermissions/environment/{environmentId}")
        {
            Method = Method.Patch
        };
        AddHeaders(request);
        var payload = new AuthorizeRequest()
        {
            Pipelines = new List<PipelineAuthorize>
            {
                new PipelineAuthorize
                {
                    Id = pipelineId,
                    Authorized = true
                }
            }
        };
        request.AddJsonBody(JsonSerializer.Serialize(payload,  JsonOptions.Instance));
        var response = await _client.ExecuteAsync<AuthorizeResponse>(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new DevOpsClientException($"An error occurred while trying to authorize the pipeline environment on a project ({response.ErrorMessage})");
        }
    }
    
    private void AddHeaders(RestRequest request)
    {
        request.AddHeader("Authorization", $"Basic {_pat.ToEncoded()}");
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Accept", "application/json;api-version=5.1-preview.1");
    }

}