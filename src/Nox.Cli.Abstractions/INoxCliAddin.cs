
namespace Nox.Cli.Abstractions;

public interface INoxCliAddin
{
    NoxActionMetaData Discover();
    Task BeginAsync(IDictionary<string, IVariable> inputs);
    Task<IDictionary<string, IVariable>> ProcessAsync(INoxWorkflowContext ctx);
    Task EndAsync();
}
