using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Core.Exceptions;
using Octopus.CoreParsers.Hcl;
using Sprache;

namespace Nox.Cli.Plugin.Hcl;

public class HclAddValue_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "hcl/add-value@v1",
            Author = "Jan Schutte",
            Description = "Add a value to an HCL template if it does not already exist",

            Inputs =
            {
                ["source-hcl"] = new NoxActionInput
                {
                    Id = "source",
                    Description = "The HCL template string to which to add a value",
                    Default = string.Empty,
                    IsRequired = true
                },

                ["parent-node-path"] = new NoxActionInput
                {
                    Id = "parent-node-path",
                    Description = "a Slash separated string that determines the parent node to which to add a value.",
                    Default = string.Empty,
                    IsRequired = true
                },

                ["value-to-add"] = new NoxActionInput
                {
                    Id = "value-to-add",
                    Description = "The value to add to the parent node.",
                    Default = string.Empty,
                    IsRequired = true
                }
            },
            
            Outputs =
            {
                ["result-hcl"] = new NoxActionOutput
                {
                    Id = "result-hcl",
                    Description = "The resulting HCL string after adding the specified value."
                },
            }
        };
    }

    private string? _sourceHcl;
    private string? _parentPath;
    private string? _valueToAdd;

    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _sourceHcl = inputs.Value<string>("source-hcl");
        _parentPath = inputs.Value<string>("parent-node-path");
        _valueToAdd = inputs.Value<string>("value-to-add");
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_sourceHcl) ||
            string.IsNullOrEmpty(_parentPath) ||
            string.IsNullOrEmpty(_valueToAdd))
        {
            ctx.SetErrorMessage("The Hcl add-value action was not initialized");
        }
        else
        {
            try
            {
                var template = HclParser.HclTemplate.Parse(_sourceHcl);
                if (!HclHelpers.ValueExists(template, $"{_parentPath}/{_valueToAdd}"))
                {
                    HclHelpers.AddValue(template, _parentPath, _valueToAdd);
                    outputs["result-hcl"] = template.ToString();
                }

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