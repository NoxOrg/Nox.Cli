namespace Nox.Cli.Plugin.Git.Abstractions;

public interface IGitClient
{
    Task<GitResponse> Init(string branchName = "main", bool suppressWarnings = false);
    Task<GitResponse> Add(string filePattern, bool suppressWarnings = false);
    Task<GitResponse> Commit(string message, bool suppressWarnings = false);
}