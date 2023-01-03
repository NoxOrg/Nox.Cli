using CodingSeb.ExpressionEvaluator;

namespace Nox.Cli.Actions;

public enum ActionState
{
    NotStarted,
    Success,
    Error,
}

public abstract class NoxAction : INoxAction
{
    public int Sequence { get; set; } = 0;
    public string Id { get; set; } = string.Empty;
    public string If { get; set; } = string.Empty;
    public string Job { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Uses { get; set; } = string.Empty;
    public Dictionary<string, NoxActionInput> Inputs { get; set; } = new Dictionary<string, NoxActionInput>();
    public List<(string, string)> Validate { get; set; } = new();
    public NoxActionDisplayMessage Display { get; set; } = new();
    public bool ContinueOnError { get; set; } = false;

    protected ActionState _state = ActionState.NotStarted;
    public ActionState State => _state;

    protected string _errorMessage = string.Empty;
    public string ErrorMessage => _errorMessage;

    public abstract NoxActionMetaData Discover();
    public abstract Task BeginAsync(NoxWorkflowExecutionContext ctx, IDictionary<string, object> inputs);
    public abstract Task<IDictionary<string, object>> ProcessAsync(NoxWorkflowExecutionContext ctx);
    public abstract Task EndAsync(NoxWorkflowExecutionContext ctx);

    public bool EvaluateValidate()
    {
        if (_state == ActionState.Error || _state == ActionState.NotStarted) return false;

        if (Validate.Count == 0) return true;

        var evaluator = new ExpressionEvaluator();
        var ret = true;

        foreach (var step in Validate)
        {
            switch (step.Item1)
            {
                case "that":
                    ret = (bool)evaluator.Evaluate(step.Item2);
                    break;

                case "and-that":
                    ret &= (bool)evaluator.Evaluate(step.Item2);
                    break;

                case "or-that":
                    ret |= (bool)evaluator.Evaluate(step.Item2);
                    break;
            }
        }

        _state = ret ? ActionState.Success : ActionState.Error;

        return ret;
    }

    public bool EvaluateIf()
    {
        if (string.IsNullOrEmpty(If)) return true;
        var evaluator = new ExpressionEvaluator();
        return (bool)evaluator.Evaluate(If);
    }

}




