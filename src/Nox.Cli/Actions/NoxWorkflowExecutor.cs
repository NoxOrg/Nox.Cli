
using Microsoft.Extensions.Configuration;
using Nox.Cli.Configuration;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;

namespace Nox.Cli.Actions;

public class NoxWorkflowExecutor
{
    public async static Task<bool> Execute(WorkflowConfiguration workflow, IConfiguration appConfig, INoxConfiguration noxConfig, IAnsiConsole console)
    {
        console.WriteLine();
        console.MarkupLine($"[green3]Executing workflow: {workflow.Name.EscapeMarkup()}[/]");

        var watch = System.Diagnostics.Stopwatch.StartNew();
        
        var ctx = new NoxWorkflowContext(workflow, noxConfig, appConfig);

        List<NoxAction> processedActions = new();

        while (ctx.CurrentAction != null)
        {
            var taskDescription = $"Step {ctx.CurrentAction.Sequence}: {ctx.CurrentAction.Name}".EscapeMarkup();

            var formattedTaskDescription = $"[bold mediumpurple3_1]{taskDescription}[/]";

            var success = await console.Status().Spinner(Spinner.Known.Clock)
                .StartAsync(formattedTaskDescription, async ctxSpinner =>
                {
                    return await ProcessTask(console, ctx, processedActions, formattedTaskDescription);
                });

            if (!success) break;

            ctx.NextStep();
        }

        await Task.WhenAll( processedActions.Select(p => p.ActionProvider.EndAsync(ctx) ) );

        watch.Stop();

        console.WriteLine();

        console.MarkupLine($"[green3]Success! ({watch.Elapsed:hh\\:mm\\:ss})[/]");

        return true;
    }

    private static async Task<bool> ProcessTask(IAnsiConsole console, 
        NoxWorkflowContext ctx, List<NoxAction> processedActions, 
        string formattedTaskDescription)
    {
        if (ctx.CurrentAction == null) return false;

        var inputs = ctx.GetInputVariables(ctx.CurrentAction);

        if (!ctx.CurrentAction.EvaluateIf())
        {
            console.WriteLine();
            console.MarkupLine(formattedTaskDescription);
            console.MarkupLine($"{Emoji.Known.ThumbsUp} Skipped because {ctx.CurrentAction.If.EscapeMarkup()} failed");
            return true;
        }

        await ctx.CurrentAction.ActionProvider.BeginAsync(ctx, inputs);

        var outputs = await ctx.CurrentAction.ActionProvider.ProcessAsync(ctx);

        ctx.StoreOutputVariables(ctx.CurrentAction, outputs);

        ctx.CurrentAction.EvaluateValidate();

        processedActions.Add(ctx.CurrentAction);

        console.WriteLine();

        console.MarkupLine(formattedTaskDescription);

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
                return false;
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(ctx.CurrentAction.Display?.Success))
            {
                console.MarkupLine($"{Emoji.Known.CheckBoxWithCheck} {ctx.CurrentAction.Display.Success.EscapeMarkup()}");
            }
        }

        return true;
    }
}




