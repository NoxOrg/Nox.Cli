using System.ServiceModel;
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

public class FolderRenameTests
{
    [Fact]
    public async Task Can_Rename_a_Folder()
    {
        var path = "./files/rename";
        //Copy the sample folder for the test
        if (Directory.Exists(Path.Combine(path, "Sample-Before")))
        {
            Directory.Delete(Path.Combine(path, "Sample-Before"));    
        }

        if (Directory.Exists(Path.Combine(path, "Sample-After")))
        {
            FileHelpers.PurgeFolderRecursive(Path.Combine(path, "Sample-After"), true);
        }
        
        CopyRecursively(Path.Combine(path, "Sample"), Path.Combine(path, "Sample-Before"));
        
        var plugin = new FileRenameFolder_v1();
        var inputs = new Dictionary<string, object>
        {
            {"source-path", "./files/rename/Sample-Before"},
            {"new-name", "Sample-After"}
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
        Assert.True(System.IO.Directory.Exists(Path.Combine(path, "Sample-After")));
        Assert.False(System.IO.Directory.Exists(Path.Combine(path, "Sample-Before")));
        Assert.Single(Directory.GetFiles(Path.Combine(path, "Sample-After")));
    }
    
    private void CopyRecursively(string sourcePath, string targetPath)
    {
        //Now Create all the directories
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        Directory.CreateDirectory(targetPath);

        //Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories))
        {
            System.IO.File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }
}