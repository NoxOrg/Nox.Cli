using Nox.Cli.Abstractions;

namespace Nox.Cli.Variables;

public interface IServerVariableProvider
{
    void SaveOutputs(string actionId, IDictionary<string, object> values);
    IDictionary<string, object> ResolveInputs(INoxAction action);
    IDictionary<string, object?> GetUnresolvedVariables();
}