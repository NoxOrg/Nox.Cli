using System.Collections;
using Nox.Cli.Abstractions;
using Nox.Cli.Actions;

namespace Nox.Cli;

public static class JobExtensions
{
    public static INoxJob Clone(this INoxJob source, string id)
    {
        var jobId = source.Id + id;
        var result = new NoxJob
        {
            Id = jobId,
            Name = source.Name,
            Steps = CloneSteps(source.Steps, jobId),
            Display = CloneDisplay(source.Display),
        };

        return result;
    }

    private static IDictionary<string, INoxAction> CloneSteps(IDictionary<string, INoxAction> sourceSteps, string jobId)
    {
        var result = new Dictionary<string, INoxAction>();
        foreach (var sourceStep in sourceSteps)
        {
            result.Add(sourceStep.Key, new NoxAction
            {
                Id = sourceStep.Value.Id,
                Display = CloneDisplay(sourceStep.Value.Display),
                Name = sourceStep.Value.Name,
                If = sourceStep.Value.If,
                ActionProvider = sourceStep.Value.ActionProvider,
                Sequence = sourceStep.Value.Sequence,
                ContinueOnError = sourceStep.Value.ContinueOnError,
                RunAtServer = sourceStep.Value.RunAtServer,
                Uses = sourceStep.Value.Uses,
                JobId = jobId,
                State = sourceStep.Value.State,
                Validate = sourceStep.Value.Validate,
                Inputs = CloneInputs(sourceStep.Value.Inputs)
            });
        }

        return result;
    }

    private static NoxActionDisplayMessage? CloneDisplay(NoxActionDisplayMessage? sourceDisplay)
    {
        if (sourceDisplay == null) return null;
        return new NoxActionDisplayMessage
        {
            Error = sourceDisplay.Error,
            Success = sourceDisplay.Success,
            IfCondition = sourceDisplay.IfCondition
        };
    }
    
    private static NoxJobDisplayMessage? CloneDisplay(NoxJobDisplayMessage? sourceDisplay)
    {
        if (sourceDisplay == null) return null;
        return new NoxJobDisplayMessage
        {
            Success = sourceDisplay.Success,
            IfCondition = sourceDisplay.IfCondition
        };
    }

    private static Dictionary<string, NoxActionInput> CloneInputs(Dictionary<string, NoxActionInput> sourceInputs)
    {
        var result = new Dictionary<string, NoxActionInput>();
        foreach (var sourceInput in sourceInputs)
        {
            result.Add(sourceInput.Key, new NoxActionInput
            {
                Id = sourceInput.Value.Id,
                Default = sourceInput.Value.Default,
                Description = sourceInput.Value.Description,
                DeprecationMessage = sourceInput.Value.DeprecationMessage,
                IsRequired = sourceInput.Value.IsRequired
            });
        }
        return result;
    }
}