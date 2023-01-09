
namespace Nox.Cli;

public interface INoxCliAddin
{
    NoxActionMetaData Discover();
    Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string, object> inputs);
    Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx);
    Task EndAsync(INoxWorkflowContext ctx);
}
