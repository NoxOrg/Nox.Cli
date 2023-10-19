using System.Collections;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Secrets;
using Nox.Cli.Server.Integration;
using Nox.Cli.Variables;
using Nox.Secrets.Abstractions;
using Nox.Solution;
using Spectre.Console;
using Spectre.Console.Cli;
using ActionState = Nox.Cli.Abstractions.ActionState;

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

    public async Task<bool> Execute(IWorkflowConfiguration workflow, IRemainingArguments arguments)
    {
        var workflowDescription = $"[seagreen1]Executing workflow: {workflow.Name.EscapeMarkup()}[/]";
        _console.WriteLine();
        _console.MarkupLine(workflowDescription);

        var watch = System.Diagnostics.Stopwatch.StartNew();

        var success = false;
        
        //Arguments
        var forceLocal = arguments.Parsed.Contains("force-local");

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

            if (workflowCtx.CurrentJob.ForEach != null && !string.IsNullOrWhiteSpace(workflowCtx.CurrentJob.ForEach.ToString()))
            {
                success = await ProcessForEachJob(workflowCtx, forceLocal);
            }
            else
            {
                success = await ProcessSingleJob(workflowCtx, forceLocal);    
            }
            
            if (!success) break;
            
            workflowCtx.NextJob();
        }

        await Task.WhenAll(_processedActions.Where(p => p.RunAtServer == false).Select(p => p.ActionProvider.EndAsync()));

        watch.Stop();

        _console.WriteLine();

        if (success)
        {
            _console.MarkupLine($"[seagreen1]Success! ({watch.Elapsed:hh\\:mm\\:ss})[/]");
            return true;
        }
        else
        {
            _console.MarkupLine($"[indianred1]Workflow halted with an error. ({watch.Elapsed:hh\\:mm\\:ss})[/]");
            return false;
        }
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

    private async Task<bool> ProcessSingleJob(NoxWorkflowContext context, bool forceLocal = false)
    {
        var job = context.CurrentJob!;
        await context.ResolveJobVariables(job);
            
        var jobName = job.Name.EscapeMarkup();
        
        _console.WriteLine();
        if (job.Steps.Any(s => s.Value.RunAtServer == true))
        {
            ConsoleRootLine($"[mediumpurple3_1]{jobName}[/] {Emoji.Known.DesktopComputer} [bold yellow]Running at: {_serverIntegration!.Endpoint}[/]");
        }
        else
        {
            ConsoleRootLine($"[mediumpurple3_1]{jobName}[/]");
        }

        var jobSkipped = false;

        if (!job.EvaluateIf())
        {
            jobSkipped = true;
            if (string.IsNullOrWhiteSpace(job.Display?.IfCondition))
            {
                ConsoleRootLine($"{Emoji.Known.BlueSquare} [deepskyblue1]Skipped because an if condition evaluated true[/]");
            }
            else
            {
                ConsoleRootLine($"{Emoji.Known.BlueSquare} [deepskyblue1]{job.Display.IfCondition.EscapeMarkup()}[/]");
            }
        }
        else
        {
            while (context.CurrentAction != null)
            {
                if (context.CancellationToken != null)
                {
                    break;
                }

                if (context.CurrentAction.RunAtServer == true && forceLocal) context.CurrentAction.RunAtServer = false;

                var taskDescription = $"{context.CurrentAction.Name}".EscapeMarkup();

                bool success;
                if (context.CurrentAction.RunAtServer == true)
                {
                    success = await _console
                        .Status()
                        .Spinner(Spinner.Known.Clock)
                        .StartAsync(taskDescription, async _ => await ProcessServerTask(context, taskDescription));
                }
                else
                {
                    var requiresConsole = context.CurrentAction.ActionProvider.Discover().RequiresConsole;
                    if (requiresConsole)
                    {
                        success = await ProcessTask(context, taskDescription);
                    }
                    else
                    {

                        success = await _console
                            .Status()
                            .Spinner(Spinner.Known.Clock)
                            .StartAsync(taskDescription, async _ => await ProcessTask(context, taskDescription));
                    }
                }

                if (!success) return false;

                context.NextStep();
            }
        }

        if (job.Display != null &&
            !string.IsNullOrWhiteSpace(job.Display.Success) &&
            !jobSkipped)
        {
            ConsoleRootLine($"{Emoji.Known.GreenSquare} [lightgreen]{job.Display.Success}[/]");
        }

        return true;
    }

    private async Task<bool> ProcessForEachJob(NoxWorkflowContext context, bool forceLocal = false)
    {
        var parentJob = context.CurrentJob!;
        var jobId = parentJob.Id;
        await context.ResolveJobVariables(parentJob);
       
        if (parentJob.ForEach is not IList forEachList) throw new NoxCliException("The value of the for-each in a Nox Job must implement IList.");
        
        foreach (var iteration in forEachList)
        {
            var jobInstance = context.ParseJob(jobId, parentJob.Sequence);
            await context.ResolveJobVariables(jobInstance);
            var varProvider = new ForEachVariableProvider(jobInstance);
            varProvider.ResolveAll(jobInstance, iteration);
            context.SetJob(jobInstance);
            
            var jobName = jobInstance.Name.EscapeMarkup();

            _console.WriteLine();
            if (jobInstance.Steps.Any(s => s.Value.RunAtServer == true))
            {
                ConsoleRootLine($"[mediumpurple3_1]{jobName}[/] {Emoji.Known.DesktopComputer} [bold yellow]Running at: {_serverIntegration!.Endpoint}[/]");
            }
            else
            {
                ConsoleRootLine($"[mediumpurple3_1]{jobName}[/]");
            }

            var jobSkipped = false;

            if (!jobInstance.EvaluateIf())
            {
                jobSkipped = true;
                if (string.IsNullOrWhiteSpace(jobInstance.Display?.IfCondition))
                {
                    ConsoleRootLine($"{Emoji.Known.BlueSquare} [deepskyblue1]Skipped because an if condition evaluated true[/]");
                }
                else
                {
                    ConsoleRootLine($"{Emoji.Known.BlueSquare} [deepskyblue1]{jobInstance.Display.IfCondition.EscapeMarkup()}[/]");
                }
            }
            else
            {
                while (context.CurrentAction != null)
                {
                    if (context.CancellationToken != null)
                    {
                        break;
                    }

                    context.CurrentAction.RunAtServer = !forceLocal;

                    var taskDescription = $"{context.CurrentAction.Name}".EscapeMarkup();

                    bool success;
                    if (context.CurrentAction.RunAtServer == true)
                    {
                        success = await _console
                            .Status()
                            .Spinner(Spinner.Known.Clock)
                            .StartAsync(taskDescription, async _ => await ProcessServerTask(context, taskDescription));
                    }
                    else
                    {
                        var requiresConsole = context.CurrentAction.ActionProvider.Discover().RequiresConsole;
                        if (requiresConsole)
                        {
                            success = await ProcessTask(context, taskDescription);
                        }
                        else
                        {

                            success = await _console
                                .Status()
                                .Spinner(Spinner.Known.Clock)
                                .StartAsync(taskDescription, async _ => await ProcessTask(context, taskDescription));
                        }
                    }

                    if (!success) return false;

                    context.NextStep();
                }
            }

            if (jobInstance.Display != null &&
                !string.IsNullOrWhiteSpace(jobInstance.Display.Success) &&
                !jobSkipped)
            {
                ConsoleRootLine($"{Emoji.Known.GreenSquare} [lightgreen]{jobInstance.Display.Success}[/]");
            }
            context.FirstStep();
        }
        return true;
    }

}




