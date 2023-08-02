using Nox.Cli.Abstractions.Caching;
using Nox.Secrets.Abstractions;
using Nox.Solution;

namespace Nox.Cli.Abstractions
{
    public interface INoxWorkflowContext
    {
        bool IsServer { get; }
        Guid InstanceId { get; }
        Guid WorkflowId { get; }
        ActionState State { get; }
        Task<ExecuteTaskResult> ExecuteTask(INoxAction action);
        void AddToVariables(string key, object value);
        void SetErrorMessage(string errorMessage);
        void SetState(ActionState state);
        
        INoxCliCacheManager? CacheManager { get; }
        INoxSecretsResolver? NoxSecretsResolver { get; }
        void SetProjectConfiguration(NoxSolution projectConfiguration);
    }
}