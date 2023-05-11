using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;

namespace Nox.Cli.Plugin.Core;

public class CoreConcatenateListProperty_v1: INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "core/concatenate-list-property@v1",
            Author = "Jan Schutte",
            Description = "Concatenate a string property of a list into a delimited string",

            Inputs =
            {
                ["source-list"] = new NoxActionInput {
                    Id = "source-list",
                    Description = "The list containing the string property to concatenate",
                    Default = new List<object>(),
                    IsRequired = true
                },
                
                ["property-name"] = new NoxActionInput {
                    Id = "property-name",
                    Description = "a string property to concatenate.",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["delimiter"] = new NoxActionInput {
                    Id = "delimiter",
                    Description = "The delimiter to use in the concatenated string",
                    Default = ",",
                    IsRequired = true
                }
                
            },

            Outputs =
            {
                ["result"] = new NoxActionOutput
                {
                    Id = "result",
                    Description = "The resulting concatenated string after the values have been replaced"
                },
            }
        };
    }

    private IList<object>? _source_list;
    private string? _propertyName;
    private string? _delimiter;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _source_list = inputs.Value<List<object>>("source-list");
        _propertyName = inputs.Value<string>("property-name");
        _delimiter = inputs.ValueOrDefault<string>("delimiter", this);
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_source_list == null || 
            _source_list.Count == 0 ||
            string.IsNullOrEmpty(_propertyName) ||
            string.IsNullOrEmpty(_delimiter))
        {
            ctx.SetErrorMessage("The Core concatenate-list-property action was not initialized");
        }
        else
        {
            try
            {
                var result = "";
                foreach (var item in _source_list)
                {
                    var prop = item.GetType().GetProperty(_propertyName);
                    if (prop != null)
                    {
                        var propVal = prop.GetValue(item);
                        if (propVal != null)
                        {
                            if (!string.IsNullOrEmpty(propVal.ToString()))
                            {
                                if (result == "")
                                {
                                    result = propVal.ToString();
                                }
                                else
                                {
                                    result += _delimiter + propVal;
                                }    
                            }    
                        }
                            
                    }
                }

                outputs["result"] = result!;
                ctx.SetState(ActionState.Success);
            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage(ex.Message);
            }
        }
        
        return Task.FromResult<IDictionary<string, object>>(outputs);
    }

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }
}