using Microsoft.Extensions.DependencyInjection;
using Nox.Cli.Abstractions.Configuration;
using NUnit.Framework.Internal;

namespace Nox.Cli.Secrets.Tests;

public class PersistedSecretStoreTests
{
    [Test]
    public async Task Can_Store_and_retrieve_a_Secret()
    {
        var services = new ServiceCollection();
        services.AddPersistedSecretStore();
        var config = SecretHelpers.GetSecretConfig();
        services.AddSingleton<ISecretValidForConfiguration>(config);
        var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IPersistedSecretStore>();
        await store.SaveAsync("my-secret", "This is my secret");
        var secret = await store.LoadAsync("my-secret");
        Assert.That(secret, Is.Not.Null);
        Assert.That(secret, Is.EqualTo("This is my secret"));
    }

    [Test]
    public async Task Must_Return_Null_for_an_expired_secret()
    {
        var services = new ServiceCollection();
        services.AddPersistedSecretStore();
        var config = SecretHelpers.GetSecretConfig();
        services.AddSingleton<ISecretValidForConfiguration>(config);
        var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IPersistedSecretStore>();
        await store.SaveAsync("my-secret", "This is my secret");
        //Wait 2 seconds
        Thread.Sleep(new TimeSpan(0,0,0, 2));
        var secret = await store.LoadAsync("my-secret");
        Assert.That(secret, Is.Null);
    }
}