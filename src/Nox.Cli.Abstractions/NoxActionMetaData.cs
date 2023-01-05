namespace Nox.Cli.Actions;

public class NoxActionMetaData
{
    public string Name { get; set; } = null!;
    public string Author { get; set; } = null!;
    public string Description { get; set; } = null!;
    public IDictionary<string,NoxActionInput> Inputs { get; set; } = null!;
    public IDictionary<string,NoxActionOutput> Outputs { get; set; } = null!;
}




