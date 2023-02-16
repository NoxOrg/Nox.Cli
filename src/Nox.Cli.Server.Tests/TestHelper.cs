using System.Text.Json;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Configuration;
using Nox.Cli.Shared.DTO.Workflow;

namespace Nox.Cli.Server.Tests;

public static class TestHelper
{
    public static INoxAction GetUninitializedPingAction()
    {
        return new ServerAction
        {
            Id = "locate-server",
            Display = new NoxActionDisplayMessage
            {
                Success = "Found the DevOps server in ${{ steps.locate-server.outputs.roundtrip-time }} milliseconds",
                Error = "The DevOps server is not accessible. Are you connected to the Internet?"
            },
            Name = "Locate the DevOps server",
            Uses = "network/ping@v1",
            Inputs = new Dictionary<string, NoxActionInput>
            {
                { "host", new NoxActionInput{Id = "host", Default = "${{ config.versionControl.server }}"} }
            }
        };
    }

    public static INoxAction GetPingAction()
    {
        return new ServerAction
        {
            Id = "locate-server",
            Display = new NoxActionDisplayMessage
            {
                Success = "Found the DevOps server in ${{ steps.locate-server.outputs.roundtrip-time }} milliseconds",
                Error = "The DevOps server is not accessible. Are you connected to the Internet?"
            },
            Name = "Locate the DevOps server",
            Uses = "network/ping@v1",
            Inputs = new Dictionary<string, NoxActionInput>
            {
                { "host", new NoxActionInput{Id = "host", Default = "localhost"} }
            }
        };
    }
    
    public static INoxAction GetSecretAction()
    {
        return new ServerAction
        {
            Id = "locate-server",
            Display = new NoxActionDisplayMessage
            {
                Success = "Found the DevOps server in ${{ steps.locate-server.outputs.roundtrip-time }} milliseconds",
                Error = "The DevOps server is not accessible. Are you connected to the Internet?"
            },
            Name = "Locate the DevOps server",
            Uses = "network/ping@v1",
            Inputs = new Dictionary<string, NoxActionInput>
            {
                { "host", new NoxActionInput{Id = "host", Default = "${{ server.secrets.test-secret }}"} }
            }
        };
    }
    
    public static INoxAction GetInvalidAction()
    {
        return new ServerAction
        {
            Id = "invalid-task",
            Display = new NoxActionDisplayMessage
            {
                Success = "This is the success message",
                Error = "This is the error message"
            },
            Name = "Execute an invalid task",
            Uses = "test/invalid@v1",
            Inputs = new Dictionary<string, NoxActionInput>
            {
                { "host", new NoxActionInput{Id = "host", Default = "${{ config.versionControl.server }}"} }
            }
        };
    }

    public static IManifestConfiguration GetValidManifest()
    {
        return new ManifestConfiguration
        {
            CliCommands = new List<ICliCommandConfiguration>
            {
                new CliCommandConfiguration { Name = "Cmd1", Description = "Command 1" },
                new CliCommandConfiguration { Name = "Cmd2", Description = "Command 2" }
            },
            Authentication = new CliAuthConfiguration
            {
                provider = "azure",
                TenantId = "88155c28-f750-4013-91d3-8347ddb3daa7"
            },
            LocalTaskExecutor = new LocalTaskExecutorConfiguration
            {
                Secrets = new SecretsConfiguration
                {
                    ValidFor = new SecretsValidForConfiguration
                    {
                        Days = 0,
                        Hours = 0,
                        Minutes = 0,
                        Seconds = 30
                    },
                    Providers = new List<ISecretProviderConfiguration>
                    {
                        new SecretProviderConfiguration { Provider = "azure-keyvault", Url = "https://we-key-Nox-02.vault.azure.net/" }
                    }
                }
            },
            RemoteTaskExecutor = new RemoteTaskExecutorConfiguration
            {
                ApplicationId = "750b96e1-e772-48f8-b6b3-84bac1961d9b",
                Url = "http://localhost:8000",
                Secrets = new SecretsConfiguration
                {
                    ValidFor = new SecretsValidForConfiguration
                    {
                        Days = 0,
                        Hours = 0,
                        Minutes = 0,
                        Seconds = 30
                    },
                    Providers = new List<ISecretProviderConfiguration>
                    {
                        new SecretProviderConfiguration { Provider = "azure-keyvault", Url = "https://nox-14356B22BB785E44.vault.azure.net/" }
                    }
                }
            }
        };
    }
    
    public static INoxAction GetFirstAction()
    {
        return new ServerAction
        {
            Id = "locate-local-pc",
            Display = new NoxActionDisplayMessage
            {
                Success = "Found the Local Machine in ${{ steps.locate-local-pc.outputs.roundtrip-time }} milliseconds",
                Error = "The Local PC is not accessible!"
            },
            Name = "Locate the Local PC",
            Uses = "network/ping@v1",
            Inputs = new Dictionary<string, NoxActionInput>
            {
                { "host", new NoxActionInput{Id = "host", Default = "localhost"} }
            }
        };
    }
    
    public static INoxAction GetSecondAction()
    {
        return new ServerAction
        {
            Id = "locate-google",
            Display = new NoxActionDisplayMessage
            {
                Success = "Found Pc in ${{ steps.locate-local-pc.outputs.roundtrip-time }} milliseconds and Google in ${{ steps.locate-google.outputs.roundtrip-time }}",
                Error = "Google is not accessible!"
            },
            Name = "Locate Google",
            Uses = "test/empty-task@v1",
            Inputs = new Dictionary<string, NoxActionInput>
            {
                { "my-variable", new NoxActionInput{Id = "my-variable", Default = "${{ steps.locate-local-pc.outputs.roundtrip-time }}"} }
            }
        };
    }
}