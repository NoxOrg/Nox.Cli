using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using Nox.Cli;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Actions;
using Nox.Cli.Caching;
using Nox.Cli.Interceptors;
using Nox.Cli.Services;
using Nox.Cli.Commands;
using Nox.Cli.Extensions;
using Nox.Cli.Helpers;
using Nox.Cli.Variables.Secrets;

var isLoggingOut = (args.Length > 0 && args[0].ToLower().Equals("logout")); 

var isGettingVersion = (args.Length > 0 && args[0].ToLower().Equals("version"));

var remoteUrl = string.Empty;

var remoteUrlArg = args.FirstOrDefault(arg => arg.StartsWith("--remoteUrl="));
if (remoteUrlArg != null)
{
    remoteUrl = remoteUrlArg.Replace("--remoteUrl=", "");
}

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
services.AddNoxTokenCache();
services.AddNoxCliServices(args);
services.AddPersistedSecretStore();
services.AddOrgSecretResolver();
services.AddTransient<INoxWorkflowExecutor, NoxWorkflowExecutor>();
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
        config.AddNoxCommands(services, isOnline, remoteUrl);
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