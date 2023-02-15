using Nox.Cli.Abstractions.Extensions;
using System.Management.Automation;
using Nox.Cli.Abstractions;
using Nox.Cli.Variables;


namespace Nox.Cli.Plugins.Powershell;

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


    public Task BeginAsync(IDictionary<string, IVariable> inputs)
    {
        _pwsh = PowerShell.Create();

        _script = inputs.Value<string>("script")!;

        return Task.CompletedTask;

    }

    public Task<IDictionary<string, IVariable>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, IVariable>();

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

                outputs["result"] = new Variable(results);

                ctx.SetState(ActionState.Success);

            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage( ex.Message );
            }
        }

        return Task.FromResult((IDictionary<string, IVariable>)outputs);
    }

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }
}

