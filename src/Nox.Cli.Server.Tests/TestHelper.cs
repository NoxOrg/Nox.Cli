using System.Text.Json;
using Nox.Cli.Abstractions;
using Nox.Cli.Configuration;
using Nox.Cli.Variables;

namespace Nox.Cli.Server.Tests;

public static class TestHelper
{
    public static IDictionary<string, Variable> GetPingInputs()
    {
        return new Dictionary<string, Variable>
        {
            {"host", new Variable("https://dev.azure.com/iwgplc")}
        };
    }

    public static IActionConfiguration GetPingConfig()
    {
        return new ActionConfiguration
        {
            Id = "locate-server",
            Display = new NoxActionDisplayMessage
            {
                Success = "Found the DevOps server in ${{ steps.locate-server.outputs.roundtrip-time }} milliseconds",
                Error = "The DevOps server is not accessible. Are you connected to the Internet?"
            },
            Name = "Locate the DevOps server",
            Uses = "network/ping@v1",
            With = new Dictionary<string, object>
            {
                { "host", "${{ config.versionControl.server }}" }
            }
        };
    }
    
    public static IActionConfiguration GetInvalidConfig()
    {
        return new ActionConfiguration
        {
            Id = "invalid-task",
            Display = new NoxActionDisplayMessage
            {
                Success = "This is the success message",
                Error = "This is the error message"
            },
            Name = "Execute an invalid task",
            Uses = "test/invalid@v1",
            With = new Dictionary<string, object>
            {
                { "host", "${{ config.versionControl.server }}" }
            }
        };
    }
}