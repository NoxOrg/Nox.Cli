using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Abstractions.Helpers;
using Nox.Cli.Secrets;
using Nox.Cli.Variables;
using System.Diagnostics;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Secrets.Abstractions;
using Nox.Solution;
using Environment = System.Environment;

namespace Nox.Cli.Actions;

public class NoxWorkflowContext : INoxWorkflowContext
{
    private readonly IWorkflowConfiguration _workflow;
    private readonly IDictionary<string, INoxJob> _jobs;
    private IDictionary<string, INoxAction> _steps;
    private readonly IClientVariableProvider _varProvider;
    private readonly INoxCliCacheManager _cacheManager;
    private readonly INoxSecretsResolver? _secretsResolver;

    private int _currentJobSequence = 0;
    private INoxJob? _currentJob;
    private INoxJob? _nextJob;
    
    private int _currentActionSequence = 0;
    private INoxAction? _currentAction;
    private INoxAction? _nextAction;

    private readonly List<JobStep> _jobSteps = new List<JobStep>();

    private readonly Regex _secretsVariableRegex = new(@"\$\{\{\s*(?<variable>[\w\.\-_:]+secret[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    public INoxJob? CurrentJob => _currentJob;
    
    public INoxAction? CurrentAction => _currentAction;

    public NoxWorkflowContext(
        IWorkflowConfiguration workflow, 
        NoxSolution projectConfig,
        IOrgSecretResolver orgSecretResolver,
        INoxCliCacheManager cacheManager,
        ILocalTaskExecutorConfiguration? lteConfig,
        INoxSecretsResolver? secretsResolver)
    {
        WorkflowId = Guid.NewGuid();
        _workflow = workflow;
        _varProvider = new ClientVariableProvider(workflow, orgSecretResolver, projectConfig, lteConfig, cacheManager.Cache);
        _cacheManager = cacheManager;
        _secretsResolver = secretsResolver;
        _jobs = ParseWorkflow();
        _currentActionSequence = 0;
        _steps = new Dictionary<string, INoxAction>();
        NextJob();
    }

    public void NextJob()
    {
        _currentJobSequence++;
        _currentJob = _jobs.Select(j => j.Value).FirstOrDefault(j => j.Sequence == _currentJobSequence);
        _nextJob = _jobs.Select(j => j.Value).FirstOrDefault(j => j.Sequence == _currentJobSequence + 1);
        
        if (_currentJob != null)
        {
            _currentActionSequence = _currentJob.FirstStepSequence;
            _steps = _currentJob.Steps;
            NextStep();
        }
    }
    
    public void NextStep()
    {
        _currentAction = _steps.Select(kv => kv.Value).FirstOrDefault(a => a.Sequence == _currentActionSequence);
        _nextAction = _steps.Select(kv => kv.Value).FirstOrDefault(a => a.Sequence == _currentActionSequence + 1);
        _currentActionSequence++;
    }

    public bool IsServer => false;
    public Guid InstanceId { get; }
    public Guid WorkflowId { get; init; }
    public ActionState State { get; }

    public void SetState(ActionState state)
    {
        if (_currentAction != null)
        {
            _currentAction.State = state;
        }
    }

    public INoxWorkflowCancellationToken? CancellationToken { get; internal set; }

    public void RequestCancellation(string reason)
    {
        CancellationToken = new NoxWorkflowCancellationToken
        {
            Reason = reason
        };
    }

    public INoxCliCacheManager? CacheManager => _cacheManager;

    public INoxSecretsResolver? NoxSecretsResolver => _secretsResolver;

    public void SetProjectConfiguration(NoxSolution projectConfiguration)
    {
        _varProvider.SetProjectConfiguration(projectConfiguration);
        _varProvider.ResolveProjectVariables();
    }

    public void SetErrorMessage(string errorMessage)
    {
        if (_currentAction != null)
        {
            _currentAction.ErrorMessage = errorMessage;
        }
    }
    
    public void SetErrorMessage(INoxAction action, string errorMessage)
    {
        var varKey = $"steps.{action.Id}.error-message";
        _varProvider.SetActionVariable(action, varKey, errorMessage);
    }

    public Task<ExecuteTaskResult> ExecuteTask(INoxAction action)
    {
        throw new NotImplementedException();
    }

    public void AddToVariables(string key, object value)
    {
        _varProvider.SetVariable($"vars.{key}", value);
    }
    
    public async Task<IDictionary<string, object>> GetInputVariables(INoxAction action, bool isServer = false)
    {
        if (isServer)
        {
            await _varProvider.ResolveForServer();
        }
        else
        {
            await _varProvider.ResolveAll();
        }    
        return _varProvider.GetInputVariables(action);        
    }

    public void StoreOutputVariables(INoxAction action, IDictionary<string, object> outputs)
    {
        _varProvider.StoreOutputVariables(action, outputs);
    }

    public IDictionary<string, object> GetUnresolvedInputVariables(INoxAction action)
    {
        return _varProvider.GetUnresolvedInputVariables(action);
    }

    private Dictionary<string, INoxJob> ParseWorkflow()
    {
        var jobSequence = 1;
        var stepSequence = 1;
        var jobs = new Dictionary<string, INoxJob>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var jobConfiguration in _workflow.Jobs)
        {
            var jobKey = jobConfiguration.Id;
            
            if (jobs.ContainsKey(jobConfiguration.Id))
            {
                throw new NoxCliException($"Job Id {jobKey} exists more than once in your workflow configuration. Job Ids must be unique in a workflow configuration");
            }

            var newJob = new NoxJob
            {
                Sequence = jobSequence,
                Id = jobConfiguration.Id,
                Name = jobConfiguration.Name,
                If = jobConfiguration.If,
                Display = jobConfiguration.Display,
                FirstStepSequence = stepSequence,
                Steps = ParseSteps(jobConfiguration, ref stepSequence)
            };

            jobSequence++;
            
            if (newJob.Display != null)
            {
                if (!string.IsNullOrEmpty(newJob.Display.Success))
                {
                    newJob.Display.Success = MaskSecretsInDisplayText(newJob.Display.Success);
                }

                if (!string.IsNullOrEmpty(newJob.Display.IfCondition))
                {
                    newJob.Display.IfCondition = MaskSecretsInDisplayText(newJob.Display.IfCondition);
                }
            }

            jobs[jobKey] = newJob;
        }

        return jobs;
    }
    
    private Dictionary<string, INoxAction> ParseSteps(IJobConfiguration jobConfiguration, ref int sequence)
    {
        var steps = new Dictionary<string, INoxAction>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var step in jobConfiguration.Steps)
        {
            if (steps.ContainsKey(step.Id))
            {
                throw new NoxCliException($"Step '{step.Id} ({step.Name})' in job: '{jobConfiguration.Name}' exists more than once. Step Ids must be unique in a job configuration");
            }
            
            if (!string.IsNullOrEmpty(step.If))
            {
                if (_jobSteps.Any(js => js.StepId == step.Id))
                {
                    var errMsg = $"Step '{step.Id} ({step.Name})' in job: '{jobConfiguration.Name}' does not have a unique id.{Environment.NewLine}";
                    errMsg += $"Steps that contain an If condition must have a unique id in a Nox Workflow.{Environment.NewLine}";
                    errMsg += $"The duplicate step id exists in the following job(s):{Environment.NewLine}";
                    foreach (var jobStep in _jobSteps.Where(js => js.StepId == step.Id).ToList())
                    {
                        errMsg += $"Job: {jobStep.JobId} ({jobStep.JobName}) Step: {jobStep.StepId} ({jobStep.StepName})";
                    }
                    
                    throw new NoxCliException(errMsg);
                }
            }
            
            _jobSteps.Add(new JobStep
            {
                JobId = jobConfiguration.Id,
                JobName = jobConfiguration.Name,
                StepId = step.Id,
                StepName = step.Name
            });

            if (string.IsNullOrWhiteSpace(step.Uses))
            {
                throw new Exception($"Step: {step.Name} in job: {jobConfiguration.Name} is missing a 'uses' property");
            }

            var actionType = NoxWorkflowContextHelpers.ResolveActionProviderTypeFromUses(step.Uses);

            if (actionType == null)
            {
                throw new Exception($"Step: {step.Name} in job: {jobConfiguration.Name} uses action: {step.Uses} which was not found");
            }

            var newAction = new NoxAction()
            {
                Sequence = sequence,
                Id = step.Id,
                JobId = jobConfiguration.Id,
                Name = step.Name,
                Uses = step.Uses,
                If = step.If,
                Validate = step.Validate,
                Display = step.Display,
                RunAtServer = step.RunAtServer,
                ContinueOnError = step.ContinueOnError,
            };
            newAction.ActionProvider = (INoxCliAddin)Activator.CreateInstance(actionType)!;
            
            sequence++;

            foreach (var (withKey, withValue) in step.With)
            {
                var input = new NoxActionInput
                {
                    Id = withKey,
                    Default = withValue
                };

                newAction.Inputs.Add(withKey, input);
            }

            if (newAction.Display != null)
            {
                if (!string.IsNullOrEmpty(newAction.Display.Error))
                {
                    newAction.Display.Error = MaskSecretsInDisplayText(newAction.Display.Error);
                }

                if (!string.IsNullOrEmpty(newAction.Display.Success))
                {
                    newAction.Display.Success = MaskSecretsInDisplayText(newAction.Display.Success);
                }

                if (!string.IsNullOrEmpty(newAction.Display.IfCondition))
                {
                    newAction.Display.IfCondition = MaskSecretsInDisplayText(newAction.Display.IfCondition);
                }
            }

            steps[newAction.Id] = newAction;
        }

        return steps;
    }
    
    private string MaskSecretsInDisplayText(string input)
    {
#if DEBUG
        if(Debugger.IsAttached) return input;
#endif
        var output = input;
        var match = _secretsVariableRegex.Match(output);
        while (match.Success)
        {
            var variable = match.Groups["variable"].Value;
            output = _secretsVariableRegex.Replace(input,
                new string('*', 20)
            );
            match = _secretsVariableRegex.Match(output);
        }
        return output;
    }
    
}




