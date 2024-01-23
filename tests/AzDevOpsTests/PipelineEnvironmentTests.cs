using Nox.Cli.Plugin.AzDevOps;

namespace AzDevOpsTests;

public class PipelineEnvironmentTests
{
    public async Task Can_find_an_environment()
    {
        var addin = new AzDevOpsFindEnvironment_v1();
        await addin.BeginAsync(new Dictionary<string, object>
        {
            
        });
    }
}