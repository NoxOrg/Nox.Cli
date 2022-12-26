﻿namespace Nox.Cli.Commands;

using System.IO.Abstractions;
using Helpers;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.Operations;
using Microsoft.VisualStudio.Services.WebApi;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

public abstract class NoxCliCommand<TSettings> : AsyncCommand<TSettings> where TSettings : CommandSettings
{
    protected readonly IAnsiConsole _console;
    protected readonly IConsoleWriter _consoleWriter;
    protected readonly INoxConfiguration _noxConfiguration;
    protected readonly IConfiguration _configuration;

    public NoxCliCommand(IAnsiConsole console, IConsoleWriter consoleWriter,
        INoxConfiguration noxConfiguration, IConfiguration configuration)
    {
        _console = console;
        _consoleWriter = consoleWriter;
        _noxConfiguration = noxConfiguration;
        _configuration = configuration;
    }

    public async override Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        if (_noxConfiguration.Team is null
            || _noxConfiguration.Team.Developers is null
            || _noxConfiguration.Team.Developers.Count == 0)
        {
            throw new Exception($"The nox definition contains no 'Developers' in the 'Team' section.");
        }

        _consoleWriter.WriteHelpText("Reading: Project Definition...");

        _console.WriteLine();
        _consoleWriter.WriteInfo($"Path:");
        _console.WriteLine(_configuration["NoxCli:DesignFolder"]!);

        _console.WriteLine();
        _consoleWriter.WriteInfo($"Project:");
        _console.WriteLine(_noxConfiguration.Name);

        _console.WriteLine();
        _consoleWriter.WriteInfo("Development Team:");

        foreach (var developer in _noxConfiguration.Team.Developers)
        {
            _console.WriteLine($"{developer.UserName}");
        }

        await Task.Delay(1);

        return 0;
    }
}