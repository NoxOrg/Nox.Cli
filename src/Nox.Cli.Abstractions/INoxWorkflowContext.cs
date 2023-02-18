namespace Nox.Cli.Abstractions
{
    public interface INoxWorkflowContext
    {
        Guid InstanceId { get; }
        Guid WorkflowId { get; }
        ActionState State { get; }
        Task<ExecuteTaskResult> ExecuteTask(INoxAction action);
        void AddToVariables(string key, object value);
        void SetErrorMessage(string errorMessage);
        void SetState(ActionState state);
    }
}