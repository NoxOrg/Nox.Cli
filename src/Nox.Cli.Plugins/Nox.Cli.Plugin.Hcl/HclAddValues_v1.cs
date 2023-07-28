using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
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
                
                ["path"] = new NoxActionInput {
                    Id = "path",
                    Description = "The path to the node where the values must be added.",
                    Default = new Dictionary<string, string>(),
                    IsRequired = true
                },

                ["values-to-add"] = new NoxActionInput {
                    Id = "values-to-add",
                    Description = "a list of values to add to the HCL template.",
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
    private string? _path;
    private List<string>? _valuesToAdd;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _sourceHcl = inputs.Value<string>("source-hcl");
        _path = inputs.Value<string>("path");
        _valuesToAdd = inputs.Value<List<string>>("values-to-add");
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_sourceHcl) ||
            string.IsNullOrEmpty(_path) ||
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
                    if (!HclHelpers.ValueExists(template, $"{_path}/{item}"))
                    {
                        HclHelpers.AddValue(template, _path, item);
                        
                    }
                }
                outputs["result-hcl"] = template.ToString();

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

    private HclElement? FindChildNode(HclElement parentNode, string name)
    {
        return parentNode.Children.First(c => c.Name == name);
    }
}