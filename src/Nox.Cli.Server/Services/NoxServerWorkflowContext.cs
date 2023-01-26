using Nox.Cli.Abstractions;
using Nox.Cli.Shared.DTO.Workflow;

namespace Nox.Cli.Server.Services;

public class NoxServerWorkflowContext: INoxWorkflowContext
{
    public Guid WorkflowId { get; init; }
    private string? _errorMessage;
    private ActionState _state;

    public string? ErrorMessage => _errorMessage;
    public ActionState State => _state;

    public void AddToVariables(string key, object value)
    {
        throw new NotImplementedException();
    }

    public void SetErrorMessage(string errorMessage)
    {
        _errorMessage = errorMessage;
    }

    public void SetState(ActionState state)
    {
        _state = state;
    }
}