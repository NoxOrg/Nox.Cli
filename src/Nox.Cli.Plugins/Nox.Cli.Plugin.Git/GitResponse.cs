namespace Nox.Cli.Plugin.Git;

public class GitResponse
{
    public GitCommandStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
}