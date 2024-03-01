using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Configuration;
using Nox.Cli.Abstractions.Helpers;
using Nox.Cli.Variables;
using System.Diagnostics;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Configuration;
using Nox.Cli.Variables.Secrets;
using Nox.Secrets.Abstractions;
using Nox.Solution;
using Environment = System.Environment;

namespace Nox.Cli.Actions;

public class NoxWorkflowContext : INoxWorkflowContext
{
    private readonly WorkflowConfiguration _workflow;
    private readonly NoxJobDictionary _jobs;
    private IDictionary<string, INoxAction> _steps;
    private readonly IClientVariableProvider _varProvider;
    private readonly INoxCliCacheManager _cacheManager;
    private readonly INoxSecretsResolver? _secretsResolver;

    private int _currentJobSequence = 0;
    private INoxJob? _currentJob;
    
    private int _currentActionSequence = 0;
    private INoxAction? _currentAction;

    private readonly List<JobStep> _jobSteps = new();

    private readonly Regex _secretsVariableRegex = new(@"\$\{\{\s*(?<variable>[\w\.\-_:]+secret[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    public INoxJob? CurrentJob => _currentJob;

    public int CurrentJobIndex => _jobs.IndexOf(_currentJob!);

    public INoxAction? CurrentAction => _currentAction;

    public NoxWorkflowContext(
        WorkflowConfiguration workflow, 
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
        
        _currentJob = _jobs.FirstOrDefault(j => j.Sequence == _currentJobSequence);
        
        if (_currentJob != null)
        {
            _currentActionSequence = 1;
            _steps = _currentJob.Steps;
            NextStep();
        }
    }
    
    public void NextStep()
    {
        _currentAction = _steps.Select(kv => kv.Value).FirstOrDefault(a => a.Sequence == _currentActionSequence);
        _currentActionSequence++;
    }

    public void SetJob(INoxJob jobInstance)
    {
        _currentJob = jobInstance;
        _currentActionSequence = 1;
        _steps = _currentJob.Steps;
        NextStep();
    }

    public void FirstStep()
    {
        _currentActionSequence = 1;
        NextStep();
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
        return _varProvider.GetInputVariables(action, isServer);        
    }

    public async Task ResolveJobVariables(INoxJob job)
    {
        await _varProvider.ResolveAll();
        _varProvider.ResolveJobVariables(job);
    }

    public void StoreOutputVariables(INoxAction action, IDictionary<string, object> outputs, bool isServer = false)
    {
        _varProvider.StoreOutputVariables(action, outputs, isServer);
    }

    public IDictionary<string, object> GetUnresolvedInputVariables(INoxAction action)
    {
        return _varProvider.GetUnresolvedInputVariables(action);
    }
    
    /// <summary>
    /// Injects a job iteration of a recurring job at the current location
    /// </summary>
    public void InjectJobIteration(int index, INoxJob job)
    {
        job.Sequence = index + 1;
        if (_jobs.Count == index)
        {
            _jobs.Add(job);
        }
        else
        {
            _jobs.Insert(index, job);
            //increment the sequence of the rest of the jobs
            for (int i = index + 1; i <= _jobs.Count - 1; i++)
            {
                _jobs[i].Sequence++;
            }
        }
    }

    private NoxJobDictionary ParseWorkflow()
    {
        var jobSequence = 1;
        var jobs = new NoxJobDictionary();
        
        foreach (var jobConfiguration in _workflow.Jobs)
        {
            var jobKey = jobConfiguration.Id;
            
            if (jobs.Contains(jobConfiguration.Id))
            {
                throw new NoxCliException($"Job Id {jobKey} exists more than once in your workflow configuration. Job Ids must be unique in a workflow configuration");
            }

            var newJob = new NoxJob
            {
                Sequence = jobSequence,
                Id = jobConfiguration.Id,
                Name = jobConfiguration.Name,
                If = jobConfiguration.If,
                ForEach = jobConfiguration.ForEach,
                Display = jobConfiguration.Display,
                Steps = ParseSteps(jobConfiguration)
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
            
            jobs.Add(newJob);
        }

        return jobs;
    }

    public INoxJob ParseJob(string jobId, int sequence)
    {
        var jobConfiguration = _workflow.Jobs.FirstOrDefault(j => j.Id == jobId);
        var newJob = new NoxJob
        {
            Sequence = sequence,
            Id = new string(jobConfiguration!.Id),
            Name = new string(jobConfiguration.Name),
            If = new string(jobConfiguration.If),
            ForEach = new string(jobConfiguration.ForEach),
            Display = CloneJobDisplay(jobConfiguration.Display),
            Steps = ParseSteps(jobConfiguration, true)
        };
        return newJob;
    }
    
    private Dictionary<string, INoxAction> ParseSteps(JobConfiguration jobConfiguration, bool ignoreDuplicateSteps = false)
    {
        var sequence = 1;
        var steps = new Dictionary<string, INoxAction>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var step in jobConfiguration.Steps)
        {
            if (steps.ContainsKey(step.Id))
            {
                throw new NoxCliException($"Step '{step.Id} ({step.Name})' in job: '{jobConfiguration.Name}' exists more than once. Step Ids must be unique in a job configuration");
            }
            
            if (!string.IsNullOrEmpty(step.If) && !ignoreDuplicateSteps)
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
                Display = CloneActionDisplay(step.Display),
                RunAtServer = step.RunAtServer,
                ContinueOnError = step.ContinueOnError,
            };
            newAction.ActionProvider = (INoxCliAddin)Activator.CreateInstance(actionType)!;
            
            sequence++;

            foreach (var (withKey, withValue) in step.With)
            {
                var input = new NoxActionInput
                {
                    Id = withKey
                };
                var withType = withValue.GetType();
                if (withType == typeof(Dictionary<object, object>))
                {
                    input.Default = new Dictionary<object, object>((Dictionary<object, object>)withValue); 
                } else if (withType == typeof(List<object>))
                {
                    var newList = new List<string>();
                    foreach (var item in (List<object>)withValue)
                    {
                        newList.Add(new string(item.ToString()));
                    }

                    input.Default = newList;
                }
                else
                {
                    input.Default = withValue;
                }
                
                newAction.Inputs.Add(withKey, input!);
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

    private NoxActionDisplayMessage? CloneActionDisplay(NoxActionDisplayMessage? source)
    {
        if (source == null) return null;
        var result = new NoxActionDisplayMessage();
        if (!string.IsNullOrWhiteSpace(source.IfCondition))
        {
            result.IfCondition = new string(source.IfCondition);
        }

        if (!string.IsNullOrWhiteSpace(source.Error))
        {
            result.Error = new string(source.Error);
        }

        if (!string.IsNullOrWhiteSpace(source.Success))
        {
            result.Success = new string(source.Success);
        }
        return result;
    }

    private NoxJobDisplayMessage? CloneJobDisplay(NoxJobDisplayMessage? source)
    {
        if (source == null) return null;
        var result = new NoxJobDisplayMessage();
        if (!string.IsNullOrWhiteSpace(source.IfCondition))
        {
            result.IfCondition = new string(source.IfCondition);
        }

        if (!string.IsNullOrWhiteSpace(source.Success))
        {
            result.Success = new string(source.Success);
        }
        return result;
    }
}




