namespace Nox.Cli.PersonalAccessToken;

public class AzDevOpsPatList
{
    public string? ContinuationToken { get; set; }
    public List<AzDevOpsPat>? PatTokens { get; set; }
}