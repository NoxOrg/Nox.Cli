using Microsoft.Graph.Print.Printers.Item.Jobs;
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

        var success = true;

        var workflowCtx = _console.Status()
            .Spinner(Spinner.Known.Clock)
            .Start("Verifying the workflow script...", _ => new NoxWorkflowContext(workflow, _noxConfig, _orgSecretResolver, _cacheManager, _lteConfig, _noxSecretsResolver));

        while (workflowCtx.CurrentJob != null)
        {
            if (workflowCtx.CancellationToken != null)
            {
                _console.MarkupLine($"[yellow3]Workflow cancelled due to: {workflowCtx.CancellationToken.Reason}[/]");
                break;
            }

            var jobName = workflowCtx.CurrentJob.Name.EscapeMarkup();
            
            _console.WriteLine();
            if (workflowCtx.CurrentJob.Steps.Any(s => s.Value.RunAtServer == true))
            {
                ConsoleRootLine($"[mediumpurple3_1]{jobName}[/] {Emoji.Known.DesktopComputer} [bold yellow]Running at: {_serverIntegration!.Endpoint}[/]");
                
            }
            else
            {
                ConsoleRootLine($"[mediumpurple3_1]{jobName}[/]"); 
            }
            
            await workflowCtx.ResolveJobVariables(workflowCtx.CurrentJob);

            var jobSkipped = false;
            
            if (!workflowCtx.CurrentJob.EvaluateIf())
            {
                jobSkipped = true;
                if (string.IsNullOrWhiteSpace(workflowCtx.CurrentJob.Display?.IfCondition))
                {
                    ConsoleRootLine($"{Emoji.Known.BlueSquare} [deepskyblue1]Skipped because an if condition evaluated true[/]");
                }
                else
                {
                    ConsoleRootLine($"{Emoji.Known.BlueSquare} [deepskyblue1]{workflowCtx.CurrentJob.Display.IfCondition.EscapeMarkup()}[/]");
                }
            }
            else
            {
                while (workflowCtx.CurrentAction != null)
                {
                    if (workflowCtx.CancellationToken != null)
                    {
                        break;
                    }

                    var taskDescription = $"{workflowCtx.CurrentAction.Name}".EscapeMarkup();
                
                    if (workflowCtx.CurrentAction.RunAtServer == true)
                    {
                        success = await _console
                            .Status()
                            .Spinner(Spinner.Known.Clock)
                            .StartAsync(taskDescription, async _ => await ProcessServerTask(workflowCtx, taskDescription));
                    }
                    else
                    {
                        var requiresConsole = workflowCtx.CurrentAction.ActionProvider.Discover().RequiresConsole;
                        if (requiresConsole)
                        {
                            success = await ProcessTask(workflowCtx, taskDescription);
                        }
                        else
                        {
                        
                            success = await _console
                                .Status()
                                .Spinner(Spinner.Known.Clock)
                                .StartAsync(taskDescription, async _ => await ProcessTask(workflowCtx, taskDescription));
                        }
                    }

                    if (!success) break;

                    workflowCtx.NextStep();
                }
            }

            if (!success)
            {
                break;
            }

            if (workflowCtx.CurrentJob.Display != null && 
                !string.IsNullOrWhiteSpace(workflowCtx.CurrentJob.Display.Success) &&
                !jobSkipped)
            {
                ConsoleRootLine($"{Emoji.Known.GreenSquare} [lightgreen]{workflowCtx.CurrentJob.Display.Success}[/]");
            }

            workflowCtx.NextJob();
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

    private async Task<bool> ProcessTask(NoxWorkflowContext ctx, string taskDescription)
    {
        if (ctx.CurrentAction == null) return false;
        
        var inputs = await ctx.GetInputVariables(ctx.CurrentAction);

        if (!ctx.CurrentAction.EvaluateIf())
        {
            var skipMessage = "";
            if (!string.IsNullOrWhiteSpace(taskDescription))
            {
                skipMessage += $"{taskDescription}...";
            }
            
            if (string.IsNullOrWhiteSpace(ctx.CurrentAction.Display?.IfCondition))
            {
                skipMessage += "Skipped because, if condition evaluated true";
            }
            else
            {
                skipMessage += ctx.CurrentAction.Display.IfCondition.EscapeMarkup();
            }
            
            ConsoleStatusLine($"{Emoji.Known.BlueCircle} [deepskyblue1]{skipMessage}[/]");
            return true;
        }
        
        var unresolved = ctx.GetUnresolvedInputVariables(ctx.CurrentAction);

        if (unresolved.Any())
        {
            _console.WriteLine();
            ConsoleStatusLine($"{Emoji.Known.RedCircle} Some variables are unresolved. Did you misspell or forget to define them? Is the service.nox.yaml available?");
            foreach (var kv in unresolved)
            {
                ConsoleStatusLine($"[bold mediumpurple3_1]{kv.Key}[/]: [indianred1]{kv.Value}[/]");
            }
            return false;
        }
        
        await ctx.CurrentAction.ActionProvider.BeginAsync(inputs);
        
        var outputs = await ctx.CurrentAction.ActionProvider.ProcessAsync(ctx);

        ctx.StoreOutputVariables(ctx.CurrentAction, outputs);

        ctx.CurrentAction.EvaluateValidate();

        _processedActions.Add(ctx.CurrentAction);

        if (ctx.CurrentAction.ActionProvider.Discover().RequiresConsole)
        {
            _console.WriteLine();
            ConsoleRootLine($"[mediumpurple3_1]{ctx.CurrentJob!.Name.EscapeMarkup()}: [/]");
        }
        
        if (ctx.CurrentAction.State == ActionState.Error)
        {
            if (ctx.CurrentAction.ContinueOnError)
            {
                ConsoleStatusLine($"{Emoji.Known.GreenCircle} {ctx.CurrentAction.Display?.Error.EscapeMarkup() ?? string.Empty}");
            }
            else
            {
                ctx.SetErrorMessage(ctx.CurrentAction, ctx.CurrentAction.ErrorMessage);
                ConsoleStatusLine($"{Emoji.Known.RedCircle} [indianred1]{ctx.CurrentAction.Display?.Error.EscapeMarkup() ?? string.Empty}[/]");
                return false;
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(ctx.CurrentAction.Display?.Success))
            {
                ConsoleStatusLine($"{Emoji.Known.GreenCircle} [seagreen1]{ctx.CurrentAction.Display.Success.EscapeMarkup()}[/]");
            }
        }

        return true;
    }
    
    private async Task<bool> ProcessServerTask(NoxWorkflowContext ctx, string taskDescription)
    {
        if (ctx.CurrentAction == null) return false;

        if (!await IsServerHealthy())
        {
            ctx.SetErrorMessage(ctx.CurrentAction, "Unable to connect to Nox Cli Server.");
            ConsoleStatusLine($"{Emoji.Known.RedCircle} [indianred1]Unable to connect to Nox Cli Server, you are trying to execute an action on the Nox Cli Server, but the server endpoint is not currently available[/]");
            return false;
        }
        
        await ctx.GetInputVariables(ctx.CurrentAction, true);
        
        if (!ctx.CurrentAction.EvaluateIf())
        {
            var skipMessage = "";
            if (!string.IsNullOrWhiteSpace(taskDescription))
            {
                skipMessage += $"{taskDescription}...";
            }
            
            if (string.IsNullOrWhiteSpace(ctx.CurrentAction.Display?.IfCondition))
            {
                skipMessage += "Skipped because, if condition evaluated true";
            }
            else
            {
                skipMessage += ctx.CurrentAction.Display.IfCondition.EscapeMarkup();
            }
            
            ConsoleStatusLine($"{Emoji.Known.BlueCircle} [deepskyblue1]{skipMessage}[/]");
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
                ConsoleStatusLine($"{Emoji.Known.GreenCircle} {ctx.CurrentAction.Display?.Error.EscapeMarkup() ?? string.Empty}");
            }
            else
            {
                ctx.SetErrorMessage(ctx.CurrentAction, executeResponse.ErrorMessage!);
                ConsoleStatusLine($"{Emoji.Known.RedCircle} [indianred1]{ctx.CurrentAction.Display?.Error.EscapeMarkup() ?? string.Empty}[/]");
                return false;
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(ctx.CurrentAction.Display?.Success))
            {
                ConsoleStatusLine($"{Emoji.Known.GreenCircle} [seagreen1]{ctx.CurrentAction.Display.Success.EscapeMarkup()}[/]");
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

    private void ConsoleRootLine(string value)
    {
        _console.MarkupLine(value);
    }

    private void ConsoleStatusLine(string value)
    {
        var padding = new string(' ', 4);
        _console.MarkupLine($"{padding}{value}");
    }
}




