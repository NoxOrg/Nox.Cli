using System.Net;
using System.Text.Json;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Authentication;
using Nox.Cli.Shared.DTO.Health;
using Nox.Cli.Shared.DTO.Workflow;
using RestSharp;
using RestSharp.Authenticators.OAuth2;

namespace Nox.Cli.Server.Integration;

public class NoxCliServerIntegration: INoxCliServerIntegration
{ 
    private readonly IAuthenticator _authenticator;
    private readonly IRemoteTaskExecutorConfiguration _remoteTaskExecutorConfiguration;
    private readonly JsonSerializerOptions _serializerOptions;
    
    public NoxCliServerIntegration(IAuthenticator authenticator, IRemoteTaskExecutorConfiguration remoteTaskExecutorConfiguration)
    {
        _authenticator = authenticator;
        _remoteTaskExecutorConfiguration = remoteTaskExecutorConfiguration;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<EchoHealthResponse?> EchoHealth()
    {
        if (string.IsNullOrEmpty(_remoteTaskExecutorConfiguration.Url)) throw new Exception("NoxCliServerIntegration::EchoHealth -> ServerUrl not set");
        var client = new RestClient($"{_remoteTaskExecutorConfiguration.Url}/Health/v1/echo");

        var request = new RestRequest() { Method = Method.Get };

        request.AddHeader("Accept", "application/json");

        // Get list of files on server
        var result = await client.ExecuteAsync(request);
        if (result.StatusCode != HttpStatusCode.OK) return null;
        return JsonSerializer.Deserialize<EchoHealthResponse>(result.Content!, _serializerOptions)!;
    }

    public async Task<ExecuteTaskResult> ExecuteTask(Guid workflowId, INoxAction? action)
    {
        if (string.IsNullOrEmpty(_remoteTaskExecutorConfiguration.Url)) throw new Exception("NoxCliServerIntegration::ExecuteTask -> ServerUrl not set");
        var apiToken = await _authenticator.GetServerToken();
        var client = new RestClient($"{_remoteTaskExecutorConfiguration.Url}/Task/v1/Execute", options =>
        {
            if (!string.IsNullOrEmpty(apiToken))
            {
                options.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(apiToken, "Bearer");    
            }
        });

        var request = new RestRequest() { Method = Method.Post };
        request.AddBody(JsonSerializer.Serialize(new ExecuteTaskRequest
        {
            WorkflowId = workflowId,
            ActionConfiguration = new ServerAction
            {
                Id = action!.Id,
                Display = action!.Display,
                ContinueOnError = action!.ContinueOnError,
                If = action!.If,
                Validate = action!.Validate,
                Name = action!.Name,
                Uses = action!.Uses,
                Inputs = action.Inputs
            },
        }));

        request.AddHeader("Accept", "application/json");
        
        var result = await client.ExecuteAsync(request);
        if (result.StatusCode != HttpStatusCode.OK)
        {
            throw result.ErrorException!;
        }

        return JsonSerializer.Deserialize<ExecuteTaskResult>(result.Content!, _serializerOptions) ?? null!;
    }

    public async Task<TaskStateResponse> GetTaskState(Guid taskExecutorId)
    {
        if (string.IsNullOrEmpty(_remoteTaskExecutorConfiguration.Url)) throw new Exception("NoxCliServerIntegration::GetTaskState -> ServerUrl not set");
        var apiToken = await _authenticator.GetServerToken();
        var client = new RestClient($"{_remoteTaskExecutorConfiguration.Url}/Task/v1/GetState/{taskExecutorId}", options =>
        {
            if (!string.IsNullOrEmpty(apiToken))
            {
                options.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(apiToken, "Bearer");    
            }
        });

        var request = new RestRequest() { Method = Method.Get };

        request.AddHeader("Accept", "application/json");
  
        var result = await client.ExecuteAsync(request);
        if (result.StatusCode != HttpStatusCode.OK)
        {
            throw result.ErrorException!;
        }
        return JsonSerializer.Deserialize<TaskStateResponse>(result.Content!, _serializerOptions)!;
    }
}