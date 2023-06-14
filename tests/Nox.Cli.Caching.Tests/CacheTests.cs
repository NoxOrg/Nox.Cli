using NUnit.Framework;

namespace Nox.Cli.Caching.Tests;

public class CacheTests
{
    [Test]
    public void Cache_Should_be_null_if_fileCache_does_not_exist_and_remote_is_offline()
    {
        var manager = new NoxCliCacheBuilder("https://nonexisting.com")
            .WithCachePath("./files")
            .WithCacheFile("./files/nonexisting.json")
            .Build();
        Assert.That(manager.Cache, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(manager.Cache!.RemoteUrl, Is.EqualTo("https://nonexisting.com"));
            Assert.That(manager.Cache!.WorkflowInfo, Is.Not.Null);
            Assert.That(manager.Cache!.WorkflowInfo.Count, Is.EqualTo(0));
            Assert.That(manager.Cache!.TemplateInfo, Is.Not.Null);
            Assert.That(manager.Cache!.TemplateInfo.Count, Is.EqualTo(0));
        });
    }

    [Test]
    public void Cache_should_be_valid_if_loaded_from_fileCache_and_remote_is_offline() 
    {
        var manager = new NoxCliCacheBuilder("http://noxorg.dev")
            .WithCachePath("./files")
            .WithCacheFile("./files/NoxCliCache.json")
            .Build();
        Assert.That(manager.Cache, Is.Not.Null);
        Assert.That(manager.Cache!.RemoteUrl, Is.EqualTo("http://noxorg.dev"));
    }

    [Test]
    public void Cache_should_be_updated_if_remote_is_online()
    {
        var manager = new NoxCliCacheBuilder("http://localhost:9000")
            .WithCachePath("./files")
            .WithCacheFile("./files/NoxCliCache.json")
            .Build();
        Assert.That(manager.Cache, Is.Not.Null);
        Assert.That(manager.Cache!.RemoteUrl, Is.EqualTo("http://noxorg.dev"));
    }
}