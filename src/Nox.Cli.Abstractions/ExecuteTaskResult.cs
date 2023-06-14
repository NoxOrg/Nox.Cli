namespace Nox.Cli.Abstractions;

public class ExecuteTaskResult
{
    public Guid WorkflowId { get; set; }
    public IDictionary<string, object>? Outputs { get; set; }
    public ActionState State { get; set; } = ActionState.NotStarted;
    public string? StateName { get; internal set; } = "Not Started";
    public string? ErrorMessage { get; set; }

    public void SetState(ActionState state, string? errorMessage = null)
    {
        State = state;
        switch (state)
        {
            case ActionState.Error:
                StateName = "Error";
                break;
            case ActionState.Running:
                StateName = "Running";
                break;
            case ActionState.Success:
                StateName = "Success";
                break;
            case ActionState.NotStarted:
                StateName = "Not Started";
                break;
            case ActionState.WaitingApproval:
                StateName = "Waiting Approval";
                break;
        }
        ErrorMessage = errorMessage;
    }
}