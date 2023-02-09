using Microsoft.Extensions.Configuration;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Server.Integration;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;

namespace Nox.Cli.Actions;

public class NoxWorkflowExecutor: INoxWorkflowExecutor
{
    private readonly INoxCliServerIntegration _serverIntegration;
    private readonly List<INoxAction> _processedActions = new();
    private readonly IAnsiConsole _console;
    private readonly INoxConfiguration _noxConfig;
    private readonly IConfiguration _appConfig;
    
    public NoxWorkflowExecutor(
        INoxCliServerIntegration serverIntegration,
        IAnsiConsole console,
        INoxConfiguration noxConfig,
        IConfiguration appConfig)
    {
        _serverIntegration = serverIntegration;
        _console = console;
        _noxConfig = noxConfig;
        _appConfig = appConfig;
    }
    
    public async Task<bool> Execute(IWorkflowConfiguration workflow)
    {
        var workflowDescription = $"[seagreen1]Executing workflow: {workflow.Name.EscapeMarkup()}[/]";
        _console.WriteLine();
        _console.MarkupLine(workflowDescription);

        var watch = System.Diagnostics.Stopwatch.StartNew();

        var ctx = _console.Status()
            .Spinner(Spinner.Known.Clock)
            .Start("Verifying the workflow script...", _ => new NoxWorkflowContext(workflow, _noxConfig, _appConfig, _serverIntegration));

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
                _console.WriteLine();
                _console.MarkupLine(serverTaskDescription);
                success = await _console.Status().Spinner(Spinner.Known.Clock)
                    .StartAsync(formattedTaskDescription, async _ =>
                        await ProcessServerTask(_console, ctx, formattedTaskDescription)
                    );
            }
            else
            {
                if (requiresConsole)
                {
                    _console.WriteLine();
                    _console.MarkupLine(formattedTaskDescription);
                    success = await ProcessTask(_console, ctx);
                }
                else // show spinner
                {
                    success = await _console.Status().Spinner(Spinner.Known.Clock)
                        .StartAsync(formattedTaskDescription, async _ =>
                            await ProcessTask(_console, ctx, formattedTaskDescription)
                        );
                }
            }

            if (!success) break;

            ctx.NextStep();
        }

        await Task.WhenAll(_processedActions.Where(p => p.RunAtServer == false).Select(p => p.ActionProvider.EndAsync(ctx)));


        watch.Stop();

        _console.WriteLine();

        if (success)
        {
            _console.MarkupLine($"[seagreen1]Success! ({watch.Elapsed:hh\\:mm\\:ss})[/]");
        }
        else
        {
            _console.MarkupLine($"[indianred1]Workflow halted with an error. ({watch.Elapsed:hh\\:mm\\:ss})[/]");
        }

        return success;
    }

    private async Task<bool> ProcessTask(IAnsiConsole console, NoxWorkflowContext ctx, 
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

        _processedActions.Add(ctx.CurrentAction);

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
    
    private async Task<bool> ProcessServerTask(
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

        _processedActions.Add(ctx.CurrentAction);
        
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

    private async Task<bool> IsServerHealthy()
    {
        var result = await _serverIntegration!.EchoHealth();
        if (result == null) return false;
        return result.Name == "Nox Cli Server";
    }
}




