namespace Nox.Cli.Plugin.Git.Abstractions;

public interface IGitClient
{
    Task<GitResponse> Init(string branchName = "main");
    Task<GitResponse> Add(string filePattern);
    Task<GitResponse> Commit(string message);
}