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
            var response = ExecuteAsync("--version", false).Result;
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
    
    public async Task<GitResponse> Init(string branchName = "main", bool suppressWarnings = false)
    {
        var response = new GitResponse();
        try
        {
            return await ExecuteAsync($"init -b {branchName}", suppressWarnings);
        }
        catch (Exception ex)
        {
            response.Status = GitCommandStatus.Error;
            response.Message = ex.Message;
        }
        return response;
    }

    public async Task<GitResponse> Add(string filePattern, bool suppressWarnings = false)
    {
        var response = new GitResponse();
        try
        {
            return await ExecuteAsync($"add --all {filePattern}", suppressWarnings);
        }
        catch (Exception ex)
        {
            response.Status = GitCommandStatus.Error;
            response.Message = ex.Message;
        }
        return response;
    }

    public async Task<GitResponse> Commit(string message, bool suppressWarnings = false)
    {
        var response = new GitResponse();
        try
        {
            return await ExecuteAsync($"commit -m \"{message}\"", suppressWarnings);
        }
        catch (Exception ex)
        {
            response.Status = GitCommandStatus.Error;
            response.Message = ex.Message;
        }
        return response;
    }

    private async Task<GitResponse> ExecuteAsync(string arguments, bool suppressWarnings)
    {
        var response = new GitResponse();
        var processInfo = GetProcessStartInfo(arguments);
        using var process = new Process();
        process.StartInfo = processInfo;
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var errors = new List<string>();
        var errorLine = await process.StandardError.ReadLineAsync();
        while (!string.IsNullOrWhiteSpace(errorLine))
        {
            if (errorLine.StartsWith("warning", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!suppressWarnings) errors.Add(errorLine);                
            }
            else
            {
                errors.Add(errorLine);
            }

            errorLine = await process.StandardError.ReadLineAsync();
        }

        var error = "";
        if (errors.Count != 0)
        {
            foreach (var item in errors)
            {
                error += item + Environment.NewLine;
            }
        }
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