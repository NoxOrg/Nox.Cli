
using Hangfire.Storage.Monitoring;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Configuration;
using Nox.Cli.Abstractions;
using Nox.Cli.Authentication;
using Nox.Cli.Configuration;
using Nox.Cli.Server.Integration;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;

namespace Nox.Cli.Actions;

public class NoxWorkflowExecutor
{
    private static INoxCliServerIntegration? _serverIntegration;
    private static readonly List<INoxAction> processedActions = new();

    public async static Task<bool> Execute(
        WorkflowConfiguration workflow,
        IConfiguration appConfig, 
        INoxConfiguration noxConfig, 
        IAnsiConsole console, 
        IAuthenticator authenticator,
        INoxCliServerIntegration serverIntegration
    )
    {
        _serverIntegration = serverIntegration;
        if (!string.IsNullOrEmpty(workflow.Cli.ServerUrl)) _serverIntegration.SetServerUrl(workflow.Cli.ServerUrl);
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
            
            if (ctx.CurrentAction.RunAtServer == true)
            {
                // if (!await authenticator.Authenticate())
                // {
                //     throw new Exception("Unable to authenticate with Nox Cli Server!");
                // }
                var serverTaskDescription = $"{formattedTaskDescription} [bold yellow] -> CLI SERVER[/]";
                console.WriteLine();
                console.MarkupLine(serverTaskDescription);
                success = await console.Status().Spinner(Spinner.Known.Clock)
                    .StartAsync(formattedTaskDescription, async _ =>
                        await ProcessServerTask(console, ctx, formattedTaskDescription)
                    );
            }
            else
            {
                if (requiresConsole)
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
            }

            if (!success) break;

            ctx.NextStep();
        }

        await Task.WhenAll(processedActions.Where(p => p.RunAtServer == false).Select(p => p.ActionProvider.EndAsync(ctx)));


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

        await ctx.CurrentAction.ActionProvider.BeginAsync(inputs);
        
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
    
    private static async Task<bool> ProcessServerTask(
        IAnsiConsole console, 
        NoxWorkflowContext ctx, 
        string? formattedTaskDescription = null)
    {
        if (ctx.CurrentAction == null) return false;

        if (!await IsServerHealthy())
        {
            ctx.SetErrorMessage(ctx.CurrentAction, "Unable to connect to Nox Cli Server");
            console.MarkupLine($"{Emoji.Known.RedCircle} [indianred1]Unable to connect to Nox Cli Server[/]");
            return false;
        }

        var inputs = ctx.GetInputVariables(ctx.CurrentAction);

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
        
        var beginResult = await _serverIntegration!.BeginTask(ctx.WorkflowId, ctx.CurrentAction, inputs);
        var executeResponse = await _serverIntegration.ExecuteTask(beginResult.TaskExecutorId);
        ctx.SetState(executeResponse.State);
        var outputs = executeResponse.Outputs;

        if (outputs != null) ctx.StoreOutputVariables(ctx.CurrentAction, outputs);
        
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

    private static async Task<bool> IsServerHealthy()
    {
        var result = await _serverIntegration!.EchoHealth();
        if (result == null) return false;
        return result.Name == "Nox Cli Server";
    }
}




