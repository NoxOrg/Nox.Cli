
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Configuration;
using Nox.Cli.Configuration;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;

namespace Nox.Cli.Actions;

public class NoxWorkflowExecutor
{

    private static readonly List<NoxAction> processedActions = new();

    public async static Task<bool> Execute(WorkflowConfiguration workflow,
        IConfiguration appConfig, INoxConfiguration noxConfig, IAnsiConsole console
    )
    {
        var workflowDescription = $"[seagreen1]Executing workflow: {workflow.Name.EscapeMarkup()}[/]";
        console.WriteLine();
        console.MarkupLine(workflowDescription);

        var watch = System.Diagnostics.Stopwatch.StartNew();

        var ctx = console.Status()
            .Spinner(Spinner.Known.Clock)
            .Start("Verifying the workflow script...", _ => new NoxWorkflowContext(workflow, noxConfig, appConfig));

        bool success = true;

        while (ctx.CurrentAction != null)
        {
            var taskDescription = $"Step {ctx.CurrentAction.Sequence}: {ctx.CurrentAction.Name}".EscapeMarkup();

            var formattedTaskDescription = $"[bold mediumpurple3_1]{taskDescription}[/]";

            var requiresConsole = ctx.CurrentAction.ActionProvider.Discover().RequiresConsole;

            if(requiresConsole)
            {
                console.WriteLine();
                console.MarkupLine(formattedTaskDescription);
                success = await ProcessTask(console, ctx);
            }
            else // show spinner
            {
                success = await console.Status().Spinner(Spinner.Known.Clock)
                    .StartAsync(formattedTaskDescription, async _ =>
                        await ProcessTask(console, ctx, formattedTaskDescription)
                    );
            }

            if (!success) break;

            ctx.NextStep();
        }

        await Task.WhenAll( processedActions.Select(p => p.ActionProvider.EndAsync(ctx) ) );

        watch.Stop();

        console.WriteLine();
        
        if (success)
        {
            console.MarkupLine($"[seagreen1]Success! ({watch.Elapsed:hh\\:mm\\:ss})[/]");
        }
        else
        {
            console.MarkupLine($"[indianred1]Workflow halted with an error. ({watch.Elapsed:hh\\:mm\\:ss})[/]");
        }

        return success;

    }

    private static async Task<bool> ProcessTask(IAnsiConsole console, NoxWorkflowContext ctx, 
        string? formattedTaskDescription = null)
    {
        if (ctx.CurrentAction == null) return false;

        var inputs = ctx.GetInputVariables(ctx.CurrentAction);

        var unresolved = ctx.GetUnresolvedInputVariables(ctx.CurrentAction);

        if (unresolved.Any())
        {
            console.WriteLine();
            console.MarkupLine($"{Emoji.Known.RedCircle} Some variables are unresolved. Did you misspell or forget to define them? Is the service.nox.yaml available?");
            foreach (var kv in unresolved)
            {
                console.MarkupLine($"  [bold mediumpurple3_1]{kv.Key}[/]: [indianred1]{kv.Value}[/]");
            }
            return false;
        }

        if (!ctx.CurrentAction.EvaluateIf())
        {
            if (!string.IsNullOrWhiteSpace(formattedTaskDescription))
            {
                console.WriteLine();
                console.MarkupLine(formattedTaskDescription);
            }
            console.MarkupLine($"{Emoji.Known.BlueCircle} Skipped because {ctx.CurrentAction.If.EscapeMarkup()} is false");
            return true;
        }

        await ctx.CurrentAction.ActionProvider.BeginAsync(ctx, inputs);

        var outputs = await ctx.CurrentAction.ActionProvider.ProcessAsync(ctx);

        ctx.StoreOutputVariables(ctx.CurrentAction, outputs);

        ctx.CurrentAction.EvaluateValidate();

        processedActions.Add(ctx.CurrentAction);

        if (!string.IsNullOrWhiteSpace(formattedTaskDescription))
        {
            console.WriteLine();
            console.MarkupLine(formattedTaskDescription);
        }

        if (ctx.CurrentAction.State == ActionState.Error)
        {
            if (ctx.CurrentAction.ContinueOnError)
            {
                console.MarkupLine($"{Emoji.Known.GreenCircle} {ctx.CurrentAction.Display?.Error.EscapeMarkup() ?? string.Empty}");
            }
            else
            {
                ctx.SetErrorMessage(ctx.CurrentAction, ctx.CurrentAction.ErrorMessage);
                console.MarkupLine($"{Emoji.Known.RedCircle} [indianred1]{ctx.CurrentAction.Display?.Error.EscapeMarkup() ?? string.Empty}[/]");
                return false;
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(ctx.CurrentAction.Display?.Success))
            {
                console.MarkupLine($"{Emoji.Known.GreenCircle} {ctx.CurrentAction.Display.Success.EscapeMarkup()}");
            }
        }

        return true;
    }
}




