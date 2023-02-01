namespace Nox.Cli.Abstractions;

public interface INoxAction
{
    int Sequence { get; set; }
    string Id { get; set; }
    string? If { get; set; }
    string Job { get; set; }
    string Name { get; set; }
    string Uses { get; set; }
    
    bool? RunAtServer { get; set; }
    Dictionary<string, NoxActionInput> Inputs { get; set; }
    Dictionary<string, string>? Validate { get; set; }
    NoxActionDisplayMessage? Display { get; set; }
    bool ContinueOnError { get; set; }

    ActionState State { get; set; }
    string ErrorMessage { get; set; }
    INoxCliAddin ActionProvider { get; set; }

    public bool EvaluateValidate();

    public bool EvaluateIf();
}