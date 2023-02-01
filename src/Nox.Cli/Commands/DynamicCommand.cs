using Nox.Cli.Authentication;
using Nox.Cli.Server.Integration;

namespace Nox.Cli.Commands;

using Helpers;
using Microsoft.Extensions.Configuration;
using Nox.Cli.Actions;
using Nox.Cli.Configuration;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

public class DynamicCommand : NoxCliCommand<DynamicCommand.Settings>
{
    private readonly IAuthenticator _authenticator;
    private readonly INoxCliServerIntegration _serverIntegration;

    public DynamicCommand(
        IAnsiConsole console, 
        IConsoleWriter consoleWriter,
        INoxConfiguration noxConfiguration, 
        IConfiguration configuration, 
        IAuthenticator authenticator,
        INoxCliServerIntegration serverIntegration)
        : base(console, consoleWriter, noxConfiguration, configuration)
    {
        _authenticator = authenticator;
        _serverIntegration = serverIntegration;
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-p|--path")]
        public string DesignFolderPath { get; set; } = null!;
    }

    public async override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        await base.ExecuteAsync(context, settings);

        var workflow = (WorkflowConfiguration)context.Data!;

        return await NoxWorkflowExecutor.Execute(workflow, _configuration, _noxConfiguration, _console, _authenticator, _serverIntegration) ? 0 : 1;
    }

}

