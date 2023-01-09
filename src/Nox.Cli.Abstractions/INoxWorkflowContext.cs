namespace Nox.Cli
{
    public interface INoxWorkflowContext
    {
        void AddToVariables(string key, object value);
        void SetErrorMessage(string errorMessage);
        void SetState(ActionState state);
    }
}