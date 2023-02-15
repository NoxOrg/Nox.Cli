using Nox.Cli.Abstractions;

namespace Nox.Cli.Server.Cache;

public interface IWorkflowCache
{
    IDictionary<string, IVariable> GetWorkflow(Guid workflowId);
    void SetWorkflow(Guid workflowId, IDictionary<string, IVariable> variables);
}