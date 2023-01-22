namespace Nox.Cli.Actions;

public class NoxActionMetaData
{
    public string Name { get; set; } = null!;
    public string Author { get; set; } = null!;
    public string Description { get; set; } = null!;
    public bool RequiresConsole { get; set; } = false;
    public IDictionary<string,NoxActionInput> Inputs { get; set; } = new Dictionary<string, NoxActionInput>();
    public IDictionary<string,NoxActionOutput> Outputs { get; set; } = new Dictionary<string, NoxActionOutput>();
}




