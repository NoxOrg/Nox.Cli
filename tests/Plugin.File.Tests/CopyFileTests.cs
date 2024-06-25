using Moq;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Actions;
using Nox.Cli.Configuration;
using Nox.Cli.Plugin.File;
using Nox.Cli.Variables.Secrets;
using Nox.Secrets.Abstractions;
using Nox.Solution;
using TestHelpers;
using Xunit;

namespace Plugin.File.Tests;

public class CopyFileTests
{
    [Fact]
    public async Task Can_copy_a_File_if_destination_folder_does_not_exist()
    {
        var path = "./files/copy-file";
        //Ensure the target folder does not exist
        FileHelpers.PurgeFolderRecursive(Path.Combine(path, "new-target"), true);
        
        var plugin = new FileCopyFile_v1();
        var inputs = new Dictionary<string, object>
        {
            {"source-path", Path.Combine(path, "source/Sample.txt")},
            {"target-path", Path.Combine(path, "new-target")}
        };
        await plugin.BeginAsync(inputs);
        var wfConfig = new WorkflowConfiguration();
        var sln = Mock.Of<NoxSolution>();
        var orgResolver = Mock.Of<IOrgSecretResolver>();
        var cacheMan = Mock.Of<INoxCliCacheManager>();
        var lteConfig = Mock.Of<LocalTaskExecutorConfiguration>();
        var secretsResolver = Mock.Of<INoxSecretsResolver>();
        var ctx = new NoxWorkflowContext(wfConfig, sln, orgResolver, cacheMan, lteConfig, secretsResolver, null!);
        await plugin.ProcessAsync(ctx);
        Assert.True(System.IO.File.Exists(Path.Combine(path, "new-target/Sample.txt")));
    }
    
    [Fact]
    public async Task Must_not_copy_if_destination_folder_exists_and_overwrite_not_specified()
    {
        var path = "./files/copy-file";
        //Ensure the target folder exists
        FileHelpers.PurgeFolderRecursive(Path.Combine(path, "new-target"), true);
        Directory.CreateDirectory(Path.Combine(path, "new-target"));
        await System.IO.File.WriteAllTextAsync(Path.Combine(path, "new-target/Sample.txt"), "Hello World");
        
        var plugin = new FileCopyFile_v1();
        var inputs = new Dictionary<string, object>
        {
            {"source-path", Path.Combine(path, "source/Sample.txt")},
            {"target-path", Path.Combine(path, "new-target")}
        };
        await plugin.BeginAsync(inputs);
        var wfConfig = new WorkflowConfiguration();
        var sln = Mock.Of<NoxSolution>();
        var orgResolver = Mock.Of<IOrgSecretResolver>();
        var cacheMan = Mock.Of<INoxCliCacheManager>();
        var lteConfig = Mock.Of<LocalTaskExecutorConfiguration>();
        var secretsResolver = Mock.Of<INoxSecretsResolver>();
        var ctx = new NoxWorkflowContext(wfConfig, sln, orgResolver, cacheMan, lteConfig, secretsResolver, null!);
        await plugin.ProcessAsync(ctx);
        await plugin.EndAsync();
        Assert.True(System.IO.File.Exists(Path.Combine(path, "new-target/Sample.txt")));
        var fileSize = new FileInfo(Path.Combine(path, "new-target/Sample.txt")).Length;
        Assert.Equal(11, fileSize);
    }
    
    [Fact]
    public async Task Can_copy_a_File_if_destination_folder_exists_and_overwrite_specified()
    {
        var path = "./files/copy-file";
        //Ensure the target folder exists
        FileHelpers.PurgeFolderRecursive(Path.Combine(path, "new-target"), true);
        Directory.CreateDirectory(Path.Combine(path, "new-target"));
        await System.IO.File.WriteAllTextAsync(Path.Combine(path, "new-target/Sample.txt"), "Hello World");
        
        var plugin = new FileCopyFile_v1();
        var inputs = new Dictionary<string, object>
        {
            {"source-path", Path.Combine(path, "source/Sample.txt")},
            {"target-path", Path.Combine(path, "new-target")},
            {"is-overwrite", true}
        };
        await plugin.BeginAsync(inputs);
        var wfConfig = new WorkflowConfiguration();
        var sln = Mock.Of<NoxSolution>();
        var orgResolver = Mock.Of<IOrgSecretResolver>();
        var cacheMan = Mock.Of<INoxCliCacheManager>();
        var lteConfig = Mock.Of<LocalTaskExecutorConfiguration>();
        var secretsResolver = Mock.Of<INoxSecretsResolver>();
        var ctx = new NoxWorkflowContext(wfConfig, sln, orgResolver, cacheMan, lteConfig, secretsResolver, null!);
        await plugin.ProcessAsync(ctx);
        await plugin.EndAsync();
        Assert.True(System.IO.File.Exists(Path.Combine(path, "new-target/Sample.txt")));
        var fileSize = new FileInfo(Path.Combine(path, "new-target/Sample.txt")).Length;
        Assert.Equal(0, fileSize);
    }
    
    
}