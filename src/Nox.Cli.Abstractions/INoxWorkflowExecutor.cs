using Nox.Cli.Abstractions.Configuration;
using Spectre.Console.Cli;

namespace Nox.Cli.Abstractions;

public interface INoxWorkflowExecutor
{
    Task<bool> Execute(IWorkflowConfiguration workflow, IRemainingArguments arguments);
}