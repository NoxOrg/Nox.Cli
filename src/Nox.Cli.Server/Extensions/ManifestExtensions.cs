using System.Text.Json;
using MassTransit;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Configuration;
using RestSharp;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Nox.Cli.Server.Extensions;

public static class ManifestExtensions
{
    public static IServiceCollection AddNoxCliManifest(this IServiceCollection services, string url)
    {
        var client = new RestClient(url);

        var request = new RestRequest() { Method = Method.Get };

        request.AddHeader("Accept", "application/json");

        // Get list of files on server
        var onlineFilesJson = client.Execute(request);

        if (onlineFilesJson.Content == null) throw new ConfigurationException("Unable to load Nox Cli Manifest!");

        if (onlineFilesJson.ResponseStatus == ResponseStatus.Error)
        {
            throw new Exception($"GetOnlineWorkflows:-> {onlineFilesJson.ErrorException?.Message}");
        }
        
        var onlineFiles = JsonSerializer.Deserialize<List<RemoteFileInfo>>(onlineFilesJson.Content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        if (onlineFiles == null) throw new Exception($"GetOnlineWorkflows:-> Unable to Deserialize online files");

        var manifestInfo = onlineFiles.Single(m => m.Name.StartsWith("Manifest"));
        request.Resource = manifestInfo.Name;

        var yaml = client.Execute(request).Content;

        if (yaml == null) throw new Exception($"Couldn't download Manifest {manifestInfo.Name}");
        
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .WithTypeMapping<IManifestConfiguration, ManifestConfiguration>()
            .WithTypeMapping<ICliCommandConfiguration, CliCommandConfiguration>()
            .WithTypeMapping<ILocalTaskExecutorConfiguration, LocalTaskExecutorConfiguration>()
            .WithTypeMapping<ISecretConfiguration, SecretConfiguration>()
            .WithTypeMapping<ISecretValidForConfiguration, SecretsValidForConfiguration>()
            .WithTypeMapping<ICliAuthConfiguration, CliAuthConfiguration>()
            .WithTypeMapping<IRemoteTaskExecutorConfiguration, RemoteTaskExecutorConfiguration>()
            .Build();

        var manifest = deserializer.Deserialize<ManifestConfiguration>(yaml);
        services.AddSingleton<IManifestConfiguration>(manifest);
        return services;
    }
}