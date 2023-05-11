using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

using Nox.Cli;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Actions;
using Nox.Cli.Interceptors;
using Nox.Cli.Services;
using Nox.Cli.Commands;
using Nox.Cli.Helpers;
using Nox.Cli.Secrets;
using Nox.Utilities.Secrets;

var appConfig = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var isLoggingOut = (args.Length > 0 && args[0].ToLower().Equals("logout")); 

var isGettingVersion = (args.Length > 0 && args[0].ToLower().Equals("version"));

if (!isGettingVersion || args.Length == 0)
{
    var installedVersion = VersionChecker.GetInstalledNoxCliVersion();

    AnsiConsole.MarkupLine(@$"[bold]_  _ ____ _  _[/]");
    AnsiConsole.MarkupLine(@$"[bold]|\ | |  |  \/ [/] [gray]version {installedVersion}[/]");
    AnsiConsole.MarkupLine(@$"[bold]| \| |__| _/\_[/] [gray]Github: https://github.com/NoxOrg/Nox.Cli[/]");
    AnsiConsole.MarkupLine(@$"");
}

var isOnline = InternetChecker.CheckForInternet();

var services = new ServiceCollection();

services.AddSingleton<IFileSystem, FileSystem>();
services.AddSingleton<IConsoleWriter, ConsoleWriter>();
services.AddTransient<INoxWorkflowExecutor, NoxWorkflowExecutor>();
services.AddNoxCliServices(args);
services.AddPersistedSecretStore();
services.AddProjectSecretResolver();
services.AddOrgSecretResolver();
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

    if(!isGettingVersion && !isLoggingOut)
    {
        config.AddNoxCommands(services, isOnline, appConfig["OnlineScriptsUrl"]!);
    }

    config.AddCommand<LogoutCommand>("logout")
    .WithDescription("Logs out the NOX cli and clears the cache.");

    config.AddCommand<VersionCommand>("version")
        .WithDescription("Displays the current NOX cli version.");
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
                ParameterType = new Style().Foreground(Color.IndianRed),
                Path = new Style().Foreground(Color.IndianRed),
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