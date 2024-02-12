using System.Text.Json;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Abstractions.Helpers;
using Nox.Cli.Plugin.AzDevOps.DTO;
using Nox.Cli.Plugin.AzDevOps.Enums;
using Nox.Cli.Plugin.AzDevOps.Helpers;
using RestSharp;

namespace Nox.Cli.Plugin.AzDevOps.Clients;

public class IdentityPickerClient
{
    private readonly RestClient _client;
    private readonly string _pat;
        
    
    public IdentityPickerClient(string serverUri, string pat)
    {
        _client = new RestClient(serverUri);
        _pat = pat;
    }

    public async Task<List<IdentityPickerIdentity>?> FindIdentity(string query, IdentityType type)
    {
        var request = new RestRequest($"/_apis/identityPicker/identities")
        {
            Method = Method.Post
        };
        AddHeaders(request);
        var payload = new IdentityPickerRequest
        {
            Query = query,
            OperationScopes = new List<string>
            {
                "ims", "source"  
            },
            IdentityTypes = new List<string>
            {
                ResolveIdentityType(type)
            },
            Options = new IdentityPickerOptions
            {
                MinResults = 1,
                MaxResults = 10
            }
        };
        request.AddJsonBody(JsonSerializer.Serialize(payload,  JsonOptions.Instance));
        var response = await _client.ExecuteAsync<IdentityPickerResponse>(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new NoxCliException($"Unable to find identity: {response}");
        }

        return response.Data!.Results![0].Identities;
    }

    private void AddHeaders(RestRequest request)
    {
        request.AddHeader("Authorization", $"Basic {_pat.ToEncoded()}");
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Accept", "application/json;api-version=5.1-preview.1");
    }

    private string ResolveIdentityType(IdentityType type)
    {
        switch (type)
        {
            case IdentityType.Group:
                return "group";
            default:
                return "";
        }
    }
}