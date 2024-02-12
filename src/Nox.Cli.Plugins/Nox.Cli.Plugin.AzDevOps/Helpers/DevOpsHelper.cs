using System.Text.Json;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Abstractions.Helpers;
using Nox.Cli.Plugin.AzDevOps.DTO;
using RestSharp;

namespace Nox.Cli.Plugin.AzDevOps.Helpers;

public static class DevOpsHelper
{
    public static async Task<IdentityPickerResponse?> FindIdentity(string serverUri, string pat, string identityType, string query, List<string> properties)
    {
        var client = new RestClient(serverUri!);
                
        var request = new RestRequest($"/_apis/identityPicker/identities")
        {
            Method = Method.Post
        };
        var base64Token = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{pat}"));

        request.AddHeader("Authorization", $"Basic {base64Token}");
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Accept", "application/json;api-version=5.1-preview.1");
        var payload = new IdentityPickerRequest
        {
            Query = query,
            OperationScopes = new List<string>
            {
                "ims", "source"  
            },
            IdentityTypes = new List<string>
            {
                "group"
            },
            Options = new IdentityPickerOptions
            {
                MinResults = 1,
                MaxResults = 1
            },
            Properties = new List<string>
            {
                "entityId",
                "originId"
            }
        };
        request.AddJsonBody(JsonSerializer.Serialize(payload,  JsonOptions.Instance));
        var response = await client.ExecuteAsync<IdentityPickerResponse>(request);
        if (response.IsSuccessStatusCode)
        {
            throw new NoxCliException($"Unable to find identity: {response}");
        }

        return response.Data;
    }
    
    
}