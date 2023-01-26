using CodingSeb.ExpressionEvaluator;
using Nox.Cli.Abstractions;

namespace Nox.Cli.Actions;

public class NoxAction: INoxAction
{
    public int Sequence { get; set; } = 0;
    public string Id { get; set; } = string.Empty;
    public string? If { get; set; } = string.Empty;
    public string Job { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Uses { get; set; } = string.Empty;
    public bool? RunAtServer { get; set; } = false;
    public Dictionary<string, NoxActionInput> Inputs { get; set; } = new Dictionary<string, NoxActionInput>();
    public Dictionary<string, string>? Validate { get; set; } = new();
    public NoxActionDisplayMessage? Display { get; set; } = new();
    public bool ContinueOnError { get; set; } = false;

    public ActionState State { get; set; } = ActionState.NotStarted;
    public string ErrorMessage { get; set; } = string.Empty;
    public INoxCliAddin ActionProvider { get; set; } = null!;


    public bool EvaluateValidate()
    {
        if (State == ActionState.Error || State == ActionState.NotStarted) return false;

        if (Validate == null || Validate.Count == 0) return true;

        var evaluator = new ExpressionEvaluator();
        var ret = true;

        foreach (var (key, value) in Validate)
        {
            switch (key)
            {
                case "that":
                    ret = (bool)evaluator.Evaluate(value);
                    break;

                case "and-that":
                    ret &= (bool)evaluator.Evaluate(value);
                    break;

                case "or-that":
                    ret |= (bool)evaluator.Evaluate(value);
                    break;
            }
        }

        State = ret ? ActionState.Success : ActionState.Error;

        return ret;
    }

    public bool EvaluateIf()
    {
        if (string.IsNullOrEmpty(If)) return true;

        var evaluator = new ExpressionEvaluator();

        return (bool)evaluator.Evaluate(If);
    }

}




