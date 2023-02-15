using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Abstractions;

public interface IVariableProvider
{
    void SetVariable(string key, object value);
    void SetActionVariable(INoxAction action, string key, object value);
    IDictionary<string, IVariable> GetInputVariables(INoxAction action);
    IDictionary<string, object> GetUnresolvedInputVariables(INoxAction action);
    void StoreOutputVariables(INoxAction action, IDictionary<string, IVariable> outputs);

    object? LookupValue(string variable, bool obfuscate = false);
}