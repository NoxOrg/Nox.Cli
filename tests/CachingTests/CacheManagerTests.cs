using Nox.Cli.Caching;
using Xunit;

namespace CachingTests;

public class CacheManagerTests
{
    [Fact]
    public void Can_get_online_script_list_from_remote()
    {
        
        var cacheManager = new NoxCliCacheManager("https://noxorg.dev", null);
        cacheManager.GetOrCreateCache();
        cacheManager.UseTenantId("88155c28-f750-4013-91d3-8347ddb3daa7");
        var yamlFiles = new Dictionary<string, string>();
        cacheManager.GetOnlineWorkflowsAndManifest(yamlFiles);
        Assert.NotEmpty(yamlFiles);
    }

    [Fact]
    public void Can_get_manifest_from_remote()
    {
        var cacheManager = new NoxCliCacheManager("https://noxorg.dev", null);
        cacheManager.GetOrCreateCache();
        cacheManager.UseTenantId("88155c28-f750-4013-91d3-8347ddb3daa7");
        var yamlFiles = new Dictionary<string, string>();
        cacheManager.GetOnlineWorkflowsAndManifest(yamlFiles);
        Assert.NotEmpty(yamlFiles);
        cacheManager.ResolveManifest(yamlFiles);
        Assert.NotNull(cacheManager.Manifest);
    }
    
    [Fact]
    public void Can_get_workflows_from_remote()
    {
        var cacheManager = new NoxCliCacheManager("https://noxorg.dev", null);
        cacheManager.GetOrCreateCache();
        cacheManager.UseTenantId("88155c28-f750-4013-91d3-8347ddb3daa7");
        var yamlFiles = new Dictionary<string, string>();
        cacheManager.GetOnlineWorkflowsAndManifest(yamlFiles);
        Assert.NotEmpty(yamlFiles);
        cacheManager.ResolveWorkflows(yamlFiles);
        var workflows = cacheManager.Workflows;
        Assert.NotNull(workflows);
        Assert.NotEmpty(workflows);
    }

    [Fact]
    public void Can_get_templates_from_remote()
    {
        var cacheManager = new NoxCliCacheManager("https://noxorg.dev", null);
        cacheManager.GetOrCreateCache();
        cacheManager.UseTenantId("88155c28-f750-4013-91d3-8347ddb3daa7");
        cacheManager.GetOnlineTemplates();
        Assert.NotNull(cacheManager.Cache);
        Assert.NotNull(cacheManager.Cache.TemplateInfo);
        Assert.NotEmpty(cacheManager.Cache.TemplateInfo);
    }
    
    [Fact]
    public void Can_refresh_a_template()
    {
        var cacheManager = new NoxCliCacheManager("https://noxorg.dev", null);
        cacheManager.GetOrCreateCache();
        cacheManager.UseTenantId("88155c28-f750-4013-91d3-8347ddb3daa7");
        Assert.NotNull(cacheManager.Cache);
        Assert.NotNull(cacheManager.Cache.TemplateInfo);
        var template = cacheManager.Cache.TemplateInfo.FirstOrDefault();
        template!.ShaChecksum = "";
        cacheManager.RefreshTemplate(template.Name);
    }

}