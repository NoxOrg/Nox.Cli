using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Abstractions;

public interface IVariableProvider
{
    IDictionary<string, object> Variables { get; }
    void Initialize(IWorkflowConfiguration workflow);

    void SetVariable(string key, object value);
    void SetActionVariable(INoxAction action, string key, object value);
    IDictionary<string, object> GetInputVariables(INoxAction action);
    IDictionary<string, object> GetUnresolvedInputVariables(INoxAction action);
    void StoreOutputVariables(INoxAction action, IDictionary<string, object> outputs);

    object LookupValue(string variable);
}