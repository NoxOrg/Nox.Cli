using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Nox.Cli.Variables;

public class ForEachVariableProvider
{
    private readonly Regex _variableRegex = new(@"\$\{\{\s*(?<variable>\b(foreach)\b[\w\.\-_:]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

    private readonly Dictionary<string, object?> _variables;
    
    
    public ForEachVariableProvider(INoxJob job)
    {
        _variables = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        Initialize(job);
    }

    public void ResolveAll(INoxJob job, object forEachObject)
    {
        _variables.ResolveForEachVariables(forEachObject);
        foreach (var stepItem in job.Steps)
        {
            var step = stepItem.Value;
            step.Id = ReplaceVariable(step.Id).ToString()!;
            step.Name = ReplaceVariable(step.Name).ToString()!;
            
            foreach (var (_, input) in step.Inputs)
            {
                if (input.Default is string inputValueString)
                {
                    input.Default = ReplaceVariable(inputValueString);
                }
                else if (input.Default is List<object> inputValueList)
                {
                    for (var i = 0; i < inputValueList.Count; i++)
                    {
                        if (inputValueList[i] is string)
                        {
                            var index = inputValueList.FindIndex(n => n.Equals(inputValueList[i]));
                            inputValueList[index] = ReplaceVariable((string)inputValueList[i]);
                        }
                    }
                } else if (input.Default is List<string> inputStringList)
                {
                    for (var i = 0; i < inputStringList.Count; i++)
                    {
                        var index = inputStringList.FindIndex(n => n.Equals(inputStringList[i]));
                        inputStringList[index] = ReplaceVariable(inputStringList[i]).ToString()!;
                    }
                } 
                
                
                else if (input.Default is Dictionary<object, object> inputValueDictionary)
                {
                    for (var i = 0; i < inputValueDictionary.Count; i++)
                    {
                        var item = inputValueDictionary.ElementAt(i);
                    
                        if (item.Value is string itemValueString)
                        {
                            inputValueDictionary[item.Key] = ReplaceVariable(itemValueString);
                        }
                    }
                }
            }
            
            if (!string.IsNullOrWhiteSpace(step.If))
            {
                step.If = ReplaceVariable(step.If).ToString()!;
            }

            if (step.Display != null)
            {
                var display = step.Display;
                if (!string.IsNullOrWhiteSpace(display.Success))
                {
                    display.Success = ReplaceVariable(display.Success).ToString()!;
                }

                if (!string.IsNullOrWhiteSpace(display.IfCondition))
                {
                    display.IfCondition = ReplaceVariable(display.IfCondition).ToString()!;
                }

                if (!string.IsNullOrWhiteSpace(display.Error))
                {
                    display.Error = ReplaceVariable(display.Error).ToString()!;
                }
            }
        }

        job.Name = ReplaceVariable(job.Name).ToString()!;
        if (!string.IsNullOrWhiteSpace(job.If)) job.If = ReplaceVariable(job.If).ToString();
        if (!string.IsNullOrWhiteSpace(job.Display?.Success))
        {
            job.Display.Success = ReplaceVariable(job.Display.Success).ToString()!;
        }
    }
    
    private void Initialize(INoxJob job)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var serialized = serializer.Serialize(job);

        var matches = _variableRegex.Matches(serialized);

        var variablesTemp = matches.Select(m => m.Groups[2].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(e => e);

        foreach (var v in variablesTemp)
        {
            _variables.Add(v, null);
        }
    }
    
    private object ReplaceVariable(string value)
    {
        object result = value;

        var match = _variableRegex.Match(result.ToString()!);

        while (match.Success)
        {
            var fullPhrase = match.Groups[0].Value;

            var variable = match.Groups["variable"].Value;

            var resolvedValue = LookupValue(variable);

            if (resolvedValue?.GetType() == typeof(object))
            {
                break;
            }

            if (resolvedValue != null)
            {
                if (resolvedValue.GetType().IsSimpleType())
                {
                    result = result.ToString()!.Replace(fullPhrase, resolvedValue.ToString());
                }
                else
                {
                    if (value == fullPhrase)
                    {
                        result = resolvedValue;
                        break;
                    }

                    result = result.ToString()!.Replace(fullPhrase, "NOT-NULL");
                }
            }
            else
            {
                if (value == fullPhrase)
                {
                    break;
                }
                result = result.ToString()!.Replace(fullPhrase, "NULL");
            }

            match = _variableRegex.Match(result.ToString()!);
        }

        return result;
    }
    
    private object? LookupValue(string variable)
    {
        if (_variables.ContainsKey(variable))
        {
            var lookupVar = _variables[variable];
            if (lookupVar == null) return null;
            return _variables[variable];
        }
        return null;
    }
    
}