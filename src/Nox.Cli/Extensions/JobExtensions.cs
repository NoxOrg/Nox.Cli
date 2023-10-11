using System.Collections;
using System.Reflection;
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
        try
        {
            foreach (var sourceInput in sourceInputs)
            {
                result.Add(sourceInput.Key, new NoxActionInput
                {
                    Id = sourceInput.Value.Id,
                    Default = sourceInput.Value.Default.Clone(),
                    Description = sourceInput.Value.Description,
                    DeprecationMessage = sourceInput.Value.DeprecationMessage,
                    IsRequired = sourceInput.Value.IsRequired
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return result;
    }
    
    private static object Clone(this object objSource)
    {
        //Get the type of source object and create a new instance of that type
        var typeSource = objSource.GetType();
        if (typeSource.Name == "String") return new string(objSource.ToString()); 
        var objTarget = Activator.CreateInstance(typeSource);
        //Get all the properties of source object type
        var propertyInfo = typeSource.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        //Assign all source property to target object 's properties
        foreach (var property in propertyInfo)
        {
            //Check whether property can be written to
            if (property.CanWrite)
            {
                //check whether property type is value type, enum or string type
                if (property.PropertyType.IsValueType || property.PropertyType.IsEnum || property.PropertyType == typeof(string))
                {
                    property.SetValue(objTarget, property.GetValue(objSource, null), null);
                }
                //else property type is object/complex types, so need to recursively call this method until the end of the tree is reached
                else
                {
                    var objPropertyValue = property.GetValue(objSource, null);
                    if (objPropertyValue == null)
                    {
                        property.SetValue(objTarget, null, null);
                    }
                    else
                    {
                        property.SetValue(objTarget, objPropertyValue.Clone(), null);
                    }
                }
            }
            else
            {
                var propType = property.GetType();
                bool isDict = propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Dictionary<,>);
                if (isDict)
                {
                    Console.WriteLine("");
                }
            }
        }
        return objTarget!;
    }
}