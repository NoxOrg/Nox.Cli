using Nox.Cli.Abstractions;
using Nox.Core.Interfaces;

namespace Nox.Cli.Variables;

public interface IClientVariableProvider
{
    void SetVariable(string key, object value);
    void SetActionVariable(INoxAction action, string key, object value);
    IDictionary<string, object> GetInputVariables(INoxAction action);
    IDictionary<string, object> GetUnresolvedInputVariables(INoxAction action);
    void StoreOutputVariables(INoxAction action, IDictionary<string, object> outputs);
    void SetProjectConfiguration(IProjectConfiguration projectConfig);

    Task ResolveAll();
    Task ResolveForServer();
    Task ResolveProjectVariables();
}