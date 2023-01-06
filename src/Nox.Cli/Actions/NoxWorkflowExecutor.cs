
using Microsoft.Extensions.Configuration;
using Nox.Cli.Actions.Configuration;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;

namespace Nox.Cli.Actions;

public class NoxWorkflowExecutor
{
    public async static Task<bool> Execute(WorkflowConfiguration workflow, IConfiguration appConfig, INoxConfiguration noxConfig, IAnsiConsole console)
    {
        console.WriteLine();
        console.WriteLine($"Validating...");
        console.MarkupLine($"[green3]Workflow: {workflow.Name.EscapeMarkup()}[/]");

        var ctx = new NoxWorkflowContext(workflow, noxConfig, appConfig);

        List<NoxAction> processedActions = new();

        while (ctx.CurrentAction != null)
        {
            console.WriteLine();
            var message = $"Step {ctx.CurrentAction.Sequence}: {ctx.CurrentAction.Name}";
            console.MarkupLine($"[bold mediumpurple3_1]{message.EscapeMarkup()}[/]");

            var inputs = ctx.GetInputVariables(ctx.CurrentAction);

            if (!ctx.CurrentAction.EvaluateIf())
            {
                console.MarkupLine($"{Emoji.Known.ThumbsUp} Skipped because {ctx.CurrentAction.If.EscapeMarkup()} failed");
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
                    console.MarkupLine($"{Emoji.Known.CheckBoxWithCheck} {ctx.CurrentAction.Display?.Error.EscapeMarkup() ?? string.Empty}");
                }
                else
                {
                    ctx.SetErrorMessage(ctx.CurrentAction, ctx.CurrentAction.ErrorMessage);
                    console.MarkupLine($"{Emoji.Known.CryingFace} [bold indianred1]{ctx.CurrentAction.Display?.Error.EscapeMarkup() ?? string.Empty}[/]");
                    break;
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(ctx.CurrentAction.Display?.Success))
                {
                    console.MarkupLine($"{Emoji.Known.CheckBoxWithCheck} {ctx.CurrentAction.Display.Success.EscapeMarkup()}");
                }
            }

            ctx.NextStep();
        }

        await Task.WhenAll( processedActions.Select(p => p.ActionProvider.EndAsync(ctx) ) );

        return true;
    }

}




