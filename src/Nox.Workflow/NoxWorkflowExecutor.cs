using Nox.Cli;
using Spectre.Console;
using System.Text.Json;

namespace Nox.Workflow;

public class NoxWorkflowExecutor
{
    private readonly NoxWorkflowParameters _workflowParameters;
    private readonly IAnsiConsole _console;

    public NoxWorkflowExecutor(NoxWorkflowParameters workflowParameters, IAnsiConsole console)
    {
        _workflowParameters = workflowParameters;
        _console = console;
    }

    public async Task<bool> Execute()
    {

        _console.WriteLine();
        _console.WriteLine($"Validating...");
        _console.MarkupLine($"[green3]Workflow: {_workflowParameters.WorkflowConfiguration.Name.EscapeMarkup()}[/]");

        var ctx = new NoxWorkflowContext(_workflowParameters);

        List<NoxAction> processedActions = new();

        while (ctx.CurrentAction != null)
        {
            _console.WriteLine();
            var message = $"Step {ctx.CurrentAction.Sequence}: {ctx.CurrentAction.Name}";
            _console.MarkupLine($"[bold mediumpurple3_1]{message.EscapeMarkup()}[/]");

            var inputs = ctx.GetInputVariables(ctx.CurrentAction);

            if (!ctx.CurrentAction.EvaluateIf())
            {
                _console.MarkupLine($"{Emoji.Known.ThumbsUp} Skipped because {ctx.CurrentAction.If.EscapeMarkup()} failed");
                ctx.NextStep();
                continue;
            }

            await ctx.CurrentAction.ActionProvider.BeginAsync(ctx, inputs);

            var outputs = await ctx.CurrentAction.ActionProvider.ProcessAsync(ctx);

            ctx.StoreOutputVariables(ctx.CurrentAction, outputs);

            ctx.CurrentAction.EvaluateValidate();

            processedActions.Add(ctx.CurrentAction);

            if (ctx.CurrentAction.State == ActionState.Error)
            {
                if (ctx.CurrentAction.ContinueOnError)
                {
                    _console.MarkupLine($"{Emoji.Known.CheckBoxWithCheck} {ctx.CurrentAction.Display?.Error.EscapeMarkup() ?? string.Empty}");
                }
                else
                {
                    ctx.SetErrorMessage(ctx.CurrentAction, ctx.CurrentAction.ErrorMessage);
                    _console.MarkupLine($"{Emoji.Known.CryingFace} [bold indianred1]{ctx.CurrentAction.Display?.Error.EscapeMarkup() ?? string.Empty}[/]");
                    break;
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(ctx.CurrentAction.Display?.Success))
                {
                    _console.MarkupLine($"{Emoji.Known.CheckBoxWithCheck} {ctx.CurrentAction.Display.Success.EscapeMarkup()}");
                }
            }

            ctx.NextStep();
        }

        await Task.WhenAll( processedActions.Select(p => p.ActionProvider.EndAsync(ctx) ) );

        _console.WriteLine();
        _console.MarkupLine($"[bold mediumpurple3_1]Done.[/]");

        return true;
    }

}




