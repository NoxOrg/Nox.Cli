namespace Nox.Cli.Abstractions
{
    public interface INoxWorkflowContext
    {
        void AddToVariables(string key, object value);
        void SetErrorMessage(string errorMessage);
        void SetState(ActionState state);
    }
}