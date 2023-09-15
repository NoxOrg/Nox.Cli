using Nox.Cli.Abstractions;

namespace Nox.Cli.Shared.DTO.Workflow;

public class ServerAction: INoxAction
{
    public int Sequence { get; set; }
    public string Id { get; set; } = string.Empty;
    public string? If { get; set; }
    public string JobId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Uses { get; set; } = string.Empty;
    public bool? RunAtServer { get; set; }
    public Dictionary<string, NoxActionInput> Inputs { get; set; } = new();
    public Dictionary<string, string>? Validate { get; set; }
    public NoxActionDisplayMessage? Display { get; set; }
    public bool ContinueOnError { get; set; }
    public ActionState State { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public INoxCliAddin ActionProvider { get; set; } = null!;
    public bool EvaluateValidate()
    {
        throw new NotImplementedException();
    }

    public bool EvaluateIf()
    {
        throw new NotImplementedException();
    }
}