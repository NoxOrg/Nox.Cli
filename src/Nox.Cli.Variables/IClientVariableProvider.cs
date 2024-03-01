using Nox.Cli.Abstractions;
using Nox.Solution;

namespace Nox.Cli.Variables;

public interface IClientVariableProvider
{
    void SetVariable(string key, object value);
    void SetActionVariable(INoxAction action, string key, object value);
    IDictionary<string, object> GetInputVariables(INoxAction action, bool isServer = false);
    IDictionary<string, object> GetUnresolvedInputVariables(INoxAction action);
    void StoreOutputVariables(INoxAction action, IDictionary<string, object> outputs, bool isServer = false);
    void SetProjectConfiguration(NoxSolution projectConfig);
    Task ResolveAll();
    Task ResolveForServer();
    Task ResolveProjectVariables();

    void ResolveJobVariables(INoxJob job);
}