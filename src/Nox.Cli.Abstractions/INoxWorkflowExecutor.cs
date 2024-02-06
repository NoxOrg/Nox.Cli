using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Configuration;
using Spectre.Console.Cli;

namespace Nox.Cli.Abstractions;

public interface INoxWorkflowExecutor
{
    Task<bool> Execute(WorkflowConfiguration workflow, IRemainingArguments arguments);
}