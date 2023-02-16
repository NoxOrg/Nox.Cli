using Nox.Cli.Abstractions;

namespace Nox.Cli.Shared.DTO.Workflow;

public class ServerAction: INoxAction
{
    public int Sequence { get; set; }
    public string Id { get; set; }
    public string? If { get; set; }
    public string Job { get; set; }
    public string Name { get; set; }
    public string Uses { get; set; }
    public bool? RunAtServer { get; set; }
    public Dictionary<string, NoxActionInput> Inputs { get; set; }
    public Dictionary<string, string>? Validate { get; set; }
    public NoxActionDisplayMessage? Display { get; set; }
    public bool ContinueOnError { get; set; }
    public ActionState State { get; set; }
    public string ErrorMessage { get; set; }
    public INoxCliAddin ActionProvider { get; set; }
    public bool EvaluateValidate()
    {
        throw new NotImplementedException();
    }

    public bool EvaluateIf()
    {
        throw new NotImplementedException();
    }
}