namespace Nox.Cli.Server.Cache;

public interface IWorkflowCache
{
    IDictionary<string, object> GetWorkflow(Guid workflowId);
    void SetWorkflow(Guid workflowId, IDictionary<string, object> variables);
}