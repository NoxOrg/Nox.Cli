
namespace Nox.Cli.Abstractions;

public interface INoxCliAddin
{
    NoxActionMetaData Discover();
    Task BeginAsync(IDictionary<string, object> inputs);
    Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx);
    Task EndAsync(INoxWorkflowContext ctx);
}
