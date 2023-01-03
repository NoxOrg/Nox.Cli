
namespace Nox.Cli.Actions
{
    public interface INoxAction
    {
        bool ContinueOnError { get; set; }
        NoxActionDisplayMessage Display { get; set; }
        string Id { get; set; }
        string If { get; set; }
        Dictionary<string, NoxActionInput> Inputs { get; set; }
        string Job { get; set; }
        string Name { get; set; }
        int Sequence { get; set; }
        string Uses { get; set; }
        List<(string, string)> Validate { get; set; }

        Task BeginAsync(NoxWorkflowExecutionContext ctx, IDictionary<string, object> inputs);
        NoxActionMetaData Discover();
        Task EndAsync(NoxWorkflowExecutionContext ctx);
        Task<IDictionary<string, object>> ProcessAsync(NoxWorkflowExecutionContext ctx);

        bool EvaluateValidate();
        bool EvaluateIf();
    }
}