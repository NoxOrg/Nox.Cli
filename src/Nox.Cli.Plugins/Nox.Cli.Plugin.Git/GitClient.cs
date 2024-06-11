using System.Diagnostics;
using Nox.Cli.Plugin.Git.Abstractions;

namespace Nox.Cli.Plugin.Git;

public class GitClient: IGitClient
{
    private readonly string _workingDirectory;

    public GitClient(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
        //Verify that git is installed
        try
        {
            var response = ExecuteAsync("--version").Result;
            if (response.Status == GitCommandStatus.Error)
            {
                throw new Exception("Git executable not found!");
            }
        }
        catch
        {
            throw new Exception("Git executable not found!");
        }
        
    }
    
    public async Task<GitResponse> Init(string branchName = "main")
    {
        var response = new GitResponse();
        try
        {
            return await ExecuteAsync($"init -b {branchName}");
        }
        catch (Exception ex)
        {
            response.Status = GitCommandStatus.Error;
            response.Message = ex.Message;
        }
        return response;
    }

    public async Task<GitResponse> Add(string filePattern)
    {
        var response = new GitResponse();
        try
        {
            return await ExecuteAsync($"add --all {filePattern}");
        }
        catch (Exception ex)
        {
            response.Status = GitCommandStatus.Error;
            response.Message = ex.Message;
        }
        return response;
    }

    public async Task<GitResponse> Commit(string message)
    {
        var response = new GitResponse();
        try
        {
            return await ExecuteAsync($"commit -m \"{message}\"");
        }
        catch (Exception ex)
        {
            response.Status = GitCommandStatus.Error;
            response.Message = ex.Message;
        }
        return response;
    }

    private async Task<GitResponse> ExecuteAsync(string arguments)
    {
        var response = new GitResponse();
        var processInfo = GetProcessStartInfo(arguments);
        using var process = new Process();
        process.StartInfo = processInfo;
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (!string.IsNullOrWhiteSpace(error))
        {
            response.Status = GitCommandStatus.Error;
            response.Message = error;
        }
        else
        {
            response.Status = output.Contains("warning", StringComparison.InvariantCultureIgnoreCase) ? GitCommandStatus.Warning : GitCommandStatus.Success;
            response.Message = output;
        }
        return response;
    }
    
    private ProcessStartInfo GetProcessStartInfo(string arguments)
    {
        return new ProcessStartInfo
        {
            FileName = "git",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _workingDirectory,
            Arguments = arguments
        };
    }
}