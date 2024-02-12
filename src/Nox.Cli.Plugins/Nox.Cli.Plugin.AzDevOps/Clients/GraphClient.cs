using System.Text.Json;
using Microsoft.VisualStudio.Services.Graph.Client;
using Nox.Cli.Abstractions.Helpers;
using Nox.Cli.Plugin.AzDevOps.DTO;
using Nox.Cli.Plugin.AzDevOps.Exceptions;
using Nox.Cli.Plugin.AzDevOps.Helpers;
using RestSharp;

namespace Nox.Cli.Plugin.AzDevOps.Clients;

public class GraphClient
{
    private readonly RestClient _client;
    private readonly string _pat;
        
    
    public GraphClient(string serverUri, string pat)
    {
        var builder = new UriBuilder(new Uri(serverUri));
        builder.Host = $"vssps.{builder.Host}";
        serverUri = builder.Uri.ToString();
        
        _client = new RestClient(serverUri);
        _pat = pat;
    }

    public async Task<string?> GetDescriptor(string storageKey)
    {
        var request = new RestRequest($"/_apis/graph/descriptors/{storageKey}")
        {
            Method = Method.Get
        };
        
        AddHeaders(request);
        var response = await _client.ExecuteAsync<DescriptorResponse>(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new DevOpsClientException($"Unable to find Descriptor for storage key: {storageKey}");
        }

        return response.Data!.Value!;
    }
    
    public async Task<string?> GetDescriptor(string projectGroupDescriptor, string originId)
    {
        var request = new RestRequest($"/_apis/graph/groups")
        {
            Method = Method.Post
        };
        request.AddQueryParameter("groupDescriptors", projectGroupDescriptor);
        var payload = new AadDescriptorRequest
        {
            OriginId = originId
        };
        request.AddJsonBody(JsonSerializer.Serialize(payload,  JsonOptions.Instance));
        AddHeaders(request);
        var response = await _client.ExecuteAsync<AadDescriptorResponse>(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new DevOpsClientException($"Unable to find Descriptor for OriginId: {originId}, StorageKey:");
        }

        return response.Data!.Descriptor!;
    }
    
    public async Task<GraphGroupResult?> FindProjectGroup(string projectDescriptor, string query)
    {
        var request = new RestRequest($"/_apis/graph/groups")
        {
            Method = Method.Get
        };
        request.AddQueryParameter("scopeDescriptor", projectDescriptor);
        AddHeaders(request);
        var response = await _client.ExecuteAsync<GraphGroupPagedResponse>(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new DevOpsClientException($"Unable to find group: {query} ({response.ErrorMessage})");
        }

        foreach (var group in response.Data!.Value!)
        {
            if (group.PrincipalName!.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return group;
            }            
        }

        return null;
    }

    public async Task<bool> AddGroupMembership(string projectGroupDescriptor, string memberDescriptor)
    {
        var request = new RestRequest($"/_apis/Graph/Memberships/{memberDescriptor}/{projectGroupDescriptor}")
        {
            Method = Method.Put
        };
        AddHeaders(request);
        var response = await _client.ExecuteAsync<MembershipResponse>(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new DevOpsClientException($"An error occurred while trying to add project group membership ({response.ErrorMessage})");
        }

        if (response.Data!.ContainerDescriptor == projectGroupDescriptor && response.Data.MemberDescriptor == memberDescriptor)
        {
            return true;
        }      
        
        return false;
    }

    private void AddHeaders(RestRequest request)
    {
        request.AddHeader("Authorization", $"Basic {_pat.ToEncoded()}");
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Accept", "application/json;api-version=7.1-preview.1");
    }
}