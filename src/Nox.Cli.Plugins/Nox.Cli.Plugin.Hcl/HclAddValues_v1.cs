using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Core.Exceptions;
using Octopus.CoreParsers.Hcl;
using Sprache;


namespace Nox.Cli.Plugin.Hcl;

public class HclAddValues_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "hcl/add-values@v1",
            Author = "Jan Schutte",
            Description = "Add a number of values to an HCL template.",

            Inputs =
            {
                ["source-hcl"] = new NoxActionInput
                {
                    Id = "source",
                    Description = "The HCL template string to which to add a value",
                    Default = string.Empty,
                    IsRequired = true
                },

                ["values-to-add"] = new NoxActionInput {
                    Id = "values-to-add",
                    Description = "a list containing paths with values to add to the HCL template.",
                    Default = new List<string>(),
                    IsRequired = true
                },
            },
            
            Outputs =
            {
                ["result-hcl"] = new NoxActionOutput
                {
                    Id = "result-hcl",
                    Description = "The resulting HCL string after adding the specified values."
                },
            }
        };
    }

    private string? _sourceHcl;
    private List<string>? _valuesToAdd;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _sourceHcl = inputs.Value<string>("source-hcl");
        _valuesToAdd = inputs.Value<List<string>>("values-to-add");
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_sourceHcl) ||
            _valuesToAdd == null ||
            _valuesToAdd.Count == 0)
        {
            ctx.SetErrorMessage("The Hcl add-values action was not initialized");
        }
        else
        {
            try
            {
                var template = HclParser.HclTemplate.Parse(_sourceHcl);
                foreach (var item in _valuesToAdd)
                {
                    var itemValues = item.Split("->");
                    if (itemValues.Length != 2) throw new NoxException("values-to-add must be structured as <PATH> seperated by -> and <VALUE> eg: <PATH>=><VALUE>");
                    if (!HclHelpers.ValueExists(template, $"{itemValues[0]}/{itemValues[1]}"))
                    {
                        HclHelpers.AddValue(template, itemValues[0], itemValues[1]);
                        outputs["result-hcl"] = template.ToString();
                    }
                }

                ctx.SetState(ActionState.Success);

            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage(ex.Message);
            }
        }

        return outputs;
    }

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }

    private HclElement? FindChildNode(HclElement parentNode, string name)
    {
        return parentNode.Children.First(c => c.Name == name);
    }
}