
namespace Nox.Cli.Actions;

public interface INoxActionProvider
{
    NoxActionMetaData Discover();
    Task BeginAsync(INoxWorkflowExecutionContext ctx, IDictionary<string, object> inputs);
    Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowExecutionContext ctx);
    Task EndAsync(INoxWorkflowExecutionContext ctx);
}
