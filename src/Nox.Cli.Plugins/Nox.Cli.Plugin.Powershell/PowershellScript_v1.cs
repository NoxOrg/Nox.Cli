using Nox.Cli.Actions;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nox.Cli.Plugins.Network;

public class PowershellScript_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "powershell/script@v1",
            Author = "Andre Sharpe",
            Description = "Executes a Powershell script",

            Inputs =
            {
                ["script"] = new NoxActionInput {
                    Id = "script",
                    Description = "The Powershell script to execute",
                    Default = "$PSVersionTable.PSVersion",
                    IsRequired = true
                },
            },

            Outputs =
            {
                ["result"] = new NoxActionOutput {
                    Id = "result",
                    Description = "The result(s) of the script",
                    Value = string.Empty,
                },
            },
        };
    }


    private PowerShell _pwsh = null!;

    private string _script = null!;


    public Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string, object> inputs)
    {
        _pwsh = PowerShell.Create();

        _script = (string)inputs["script"];

        return Task.CompletedTask;

    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_pwsh == null || _script == null)
        {
            ctx.SetErrorMessage("The Powershell action was not initialized");
        }
        else
        {
            try
            {
                _pwsh.AddScript(_script.Trim());

                var results = _pwsh.Invoke();

                outputs["result"] = results;

                ctx.SetState(ActionState.Success);

            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage( ex.Message );
            }
        }

        return Task.FromResult((IDictionary<string, object>)outputs);
    }

    public Task EndAsync(INoxWorkflowContext ctx)
    {
        return Task.CompletedTask;
    }
}

