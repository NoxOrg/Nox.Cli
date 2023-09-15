using Microsoft.Extensions.Configuration;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Secrets;
using Nox.Cli.Server.Integration;
using Nox.Secrets.Abstractions;
using Nox.Solution;
using Spectre.Console;

namespace Nox.Cli.Actions;

public class NoxWorkflowExecutor: INoxWorkflowExecutor
{
    private readonly INoxCliServerIntegration? _serverIntegration;
    private readonly List<INoxAction> _processedActions = new();
    private readonly IAnsiConsole _console;
    private readonly NoxSolution _noxConfig;
    private readonly IOrgSecretResolver _orgSecretResolver;
    private readonly ILocalTaskExecutorConfiguration? _lteConfig;
    private readonly INoxCliCacheManager _cacheManager;
    private readonly INoxSecretsResolver? _noxSecretsResolver;
    
    public NoxWorkflowExecutor(
        IAnsiConsole console,
        NoxSolution noxConfig,
        IOrgSecretResolver orgSecretResolver,
        INoxCliCacheManager cacheManager,
        ILocalTaskExecutorConfiguration? lteConfig = null,
        INoxCliServerIntegration? serverIntegration = null,
        INoxSecretsResolver? noxSecretsResolver = null)
    {
        _serverIntegration = serverIntegration;
        _console = console;
        _noxConfig = noxConfig;
        _lteConfig = lteConfig;
        _orgSecretResolver = orgSecretResolver;
        _cacheManager = cacheManager;
        _noxSecretsResolver = noxSecretsResolver;
    }
    
    public async Task<bool> Execute(IWorkflowConfiguration workflow)
    {
        var workflowDescription = $"[seagreen1]Executing workflow: {workflow.Name.EscapeMarkup()}[/]";
        _console.WriteLine();
        _console.MarkupLine(workflowDescription);

        var watch = System.Diagnostics.Stopwatch.StartNew();

        var ctx = _console.Status()
            .Spinner(Spinner.Known.Clock)
            .Start("Verifying the workflow script...", _ => new NoxWorkflowContext(workflow, _noxConfig, _orgSecretResolver, _cacheManager, _lteConfig, _noxSecretsResolver));

        bool success = true;

        while (ctx.CurrentJob != null)
        {
            while (ctx.CurrentAction != null)
            {
                if (ctx.CancellationToken != null)
                {
                    _console.MarkupLine($"[yellow3]Workflow cancelled due to: {ctx.CancellationToken.Reason}[/]");
                    break;
                }
            
                var taskDescription = $"Step {ctx.CurrentAction.Sequence}: {ctx.CurrentAction.Name}".EscapeMarkup();

                var formattedTaskDescription = $"[bold mediumpurple3_1]{taskDescription}[/]";

                var requiresConsole = ctx.CurrentAction.ActionProvider.Discover().RequiresConsole;
            
                if (ctx.CurrentAction.RunAtServer == true)
                {
                    _console.WriteLine();
                    _console.MarkupLine(formattedTaskDescription);
                    _console.MarkupLine($"    {Emoji.Known.DesktopComputer} [bold yellow]Running at: {_serverIntegration!.Endpoint}[/]");
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
            ctx.NextJob();
        }

        await Task.WhenAll(_processedActions.Where(p => p.RunAtServer == false).Select(p => p.ActionProvider.EndAsync()));


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

    private async Task<bool> ProcessTask(
        IAnsiConsole console, 
        NoxWorkflowContext ctx, 
        string? formattedTaskDescription = null)
    {
        if (ctx.CurrentAction == null) return false;
        
        var inputs = await ctx.GetInputVariables(ctx.CurrentAction);

        if (!ctx.CurrentAction.EvaluateIf())
        {
            if (!string.IsNullOrWhiteSpace(formattedTaskDescription))
            {
                console.WriteLine();
                console.MarkupLine(formattedTaskDescription);
            }
            if (string.IsNullOrWhiteSpace(ctx.CurrentAction.Display?.IfCondition))
            {
                console.MarkupLine($"{Emoji.Known.BlueCircle} Skipped because, if condition proved true");
            }
            else
            {
                console.MarkupLine($"{Emoji.Known.BlueCircle} {ctx.CurrentAction.Display.IfCondition.EscapeMarkup()}");
            }
            return true;
        }
        
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
            ctx.SetErrorMessage(ctx.CurrentAction, "Unable to connect to Nox Cli Server.");
            console.MarkupLine($"{Emoji.Known.RedCircle} [indianred1]Unable to connect to Nox Cli Server, you are trying to execute an action on the Nox Cli Server, but the server endpoint is not currently available[/]");
            return false;
        }
        
        await ctx.GetInputVariables(ctx.CurrentAction, true);
        
        if (!ctx.CurrentAction.EvaluateIf())
        {
            if (!string.IsNullOrWhiteSpace(formattedTaskDescription))
            {
                console.WriteLine();
                console.MarkupLine(formattedTaskDescription);
            }

            if (string.IsNullOrWhiteSpace(ctx.CurrentAction.Display?.IfCondition))
            {
                console.MarkupLine($"{Emoji.Known.BlueCircle} Skipped because, if condition proved true");
            }
            else
            {
                console.MarkupLine($"{Emoji.Known.BlueCircle} {ctx.CurrentAction.Display.IfCondition.EscapeMarkup()}");
            }
            return true;
        }

        var executeResponse = await _serverIntegration!.ExecuteTask(ctx.WorkflowId, ctx.CurrentAction);
        ctx.SetState(executeResponse.State);
        var outputs = executeResponse.Outputs;

        if (outputs != null) ctx.StoreOutputVariables(ctx.CurrentAction, outputs);
        
        ctx.CurrentAction.EvaluateValidate();

        _processedActions.Add(ctx.CurrentAction);

        if (ctx.CurrentAction.State == ActionState.Error)
        {
            if (ctx.CurrentAction.ContinueOnError)
            {
                console.MarkupLine($"{Emoji.Known.GreenCircle} {ctx.CurrentAction.Display?.Error.EscapeMarkup() ?? string.Empty}");
            }
            else
            {
                ctx.SetErrorMessage(ctx.CurrentAction, executeResponse.ErrorMessage!);
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
        if (_serverIntegration == null) return false;
        var result = await _serverIntegration.EchoHealth();
        if (result == null) return false;
        return result.Name == "Nox Cli Server";
    }
}




