using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

using Nox.Cli;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Interceptors;
using Nox.Cli.Services;
using Nox.Cli.Commands;
using Nox.Cli.Helpers;

if ((args.Length > 0 && !args[0].Equals("version")) || args.Length == 0)
{
    var installedVersion = VersionChecker.GetInstalledNoxCliVersion();
    AnsiConsole.MarkupLine(@$"[bold red3] _ __   _____  __ [/]");
    AnsiConsole.MarkupLine(@$"[bold red3]| '_ \ / _ \ \/ / [/] Nox.Cli version {installedVersion}");
    AnsiConsole.MarkupLine(@$"[bold red3]| | | | (_) >  <  [/] [gray]Github: https://github.com/NoxOrg/Nox.Cli[/]");
    AnsiConsole.MarkupLine(@$"[bold red3]|_| |_|\___/_/\_\ [/]");
    AnsiConsole.MarkupLine(@$"[bold red3]                  [/]");
    AnsiConsole.MarkupLine(@$"Starting...");
}

var services = new ServiceCollection();
services.AddSingleton<IFileSystem, FileSystem>();
services.AddSingleton<IConsoleWriter, ConsoleWriter>();
services.AddNoxCliServices(args);
services.AddAutoMapper(Assembly.GetExecutingAssembly());

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    Console.OutputEncoding = Encoding.Unicode;
}

app.Configure(config =>
{
    config.SetApplicationName("nox");

    config.SetInterceptor(new OperatingSystemInterceptor());

    config.AddNoxCommands();

    config.AddCommand<VersionCommand>("version")
        .WithDescription("Displays the current NOX cli version.")
        .WithExample(new[] { "version" });
    
});


int returnValue = 0;

try
{
    returnValue = await app.RunAsync(args);
}
catch (Exception e)
{
    if (e is INoxCliException)
        AnsiConsole.MarkupLine($"{e.Message}");
    else
    {
        AnsiConsole.WriteException(e, new ExceptionSettings
        {
            Format = ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks,
            Style = new ExceptionStyle
            {
                Exception = new Style().Foreground(Color.Grey),
                Message = new Style().Foreground(Color.White),
                NonEmphasized = new Style().Foreground(Color.Cornsilk1),
                Parenthesis = new Style().Foreground(Color.Cornsilk1),
                Method = new Style().Foreground(Color.Red),
                ParameterName = new Style().Foreground(Color.Cornsilk1),
                ParameterType = new Style().Foreground(Color.Red),
                Path = new Style().Foreground(Color.Red),
                LineNumber = new Style().Foreground(Color.Cornsilk1),
            }
        });
        returnValue = 1;
    }
}
finally
{
    VersionChecker.CheckForLatestVersion();
}

return returnValue;