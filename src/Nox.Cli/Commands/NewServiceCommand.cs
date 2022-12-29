namespace Nox.Cli.Commands;

using System.ComponentModel;
using System.Text;
using Helpers;
using Microsoft.Extensions.Configuration;
using Nox.Core.Configuration;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class NewServiceCommand : NoxCliCommand<NewServiceCommand.Settings>
{
    public NewServiceCommand(IAnsiConsole console, IConsoleWriter consoleWriter,
        INoxConfiguration noxConfiguration, IConfiguration configuration) 
        : base(console, consoleWriter, noxConfiguration, configuration) { }

    public class Settings : CommandSettings
    {
        [CommandOption("-n|--name")]
        [Description("The name of the Nox microservice")]
        public string Name { get; set; } = null!;

        [CommandOption("-d|--description <VALUE>")]
        [Description("A string describing the microservice")]
        public string Description { get; set; } = null!;

        [CommandOption("-p|--databaseProvider <VALUE>")]
        [Description("The database provider for data storage\nAccepts postgres, sqlserver or mysql.")]
        public string DatabaseProvider { get; set; } = null!;

        [CommandOption("-f|--file <VALUE>")]
        [Description("The json file to output the definition to\nUse <serviceName>.service.nox.json per convention")]
        public string File { get; set; } = null!;
    }

    public async override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        await base.ExecuteAsync(context, settings);

        var service = new NoxConfiguration();

        _console.WriteLine();

        _consoleWriter.WriteRule("New Service");

        _console.WriteLine();

        if(string.IsNullOrWhiteSpace(settings.Name))
        {
            settings.Name = AnsiConsole.Ask<string>("[green]What's the [bold mediumpurple3_1]name[/] of your service[/]?");
        }
        service.Name = settings.Name;

        if(string.IsNullOrWhiteSpace(settings.Description))
        {
            settings.Description = AnsiConsole.Ask<string>("[green]What's the [bold mediumpurple3_1]description[/] of your service[/]?");
        }
        service.Description = settings.Description;

        service.Database = new DataSourceConfiguration();

        if (string.IsNullOrWhiteSpace(settings.DatabaseProvider))
        {
            settings.DatabaseProvider = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]What [underline mediumpurple3_1]database provider[/] will you service use for storage[/]?")
                    .AddChoices(new[] { "Postgres", "SqlServer", "MySql" })
            );
        }
        _console.MarkupLine($"[green]What [underline mediumpurple3_1]database provider[/] will you service use for storage[/]? {settings.DatabaseProvider}");
        service.Database.Provider = settings.DatabaseProvider.ToLower();

        if (string.IsNullOrWhiteSpace(settings.File))
        {
            settings.File = AnsiConsole.Ask<string>("[green]What [underline mediumpurple3_1]file[/] name would you like to save to[/]?", $"{settings.Name}.service.nox.yaml");
        }

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithAttributeOverride<NoxConfiguration>( c => c.EndpointProvider, new YamlIgnoreAttribute())
            .WithAttributeOverride<NoxConfiguration>( c => c.Apis!, new YamlIgnoreAttribute())
            .WithAttributeOverride<NoxConfiguration>( c => c.Entities!, new YamlIgnoreAttribute())
            .WithAttributeOverride<NoxConfiguration>( c => c.Loaders!, new YamlIgnoreAttribute())
            .WithAttributeOverride<NoxConfiguration>(c => c.Id, new YamlIgnoreAttribute())
            .WithAttributeOverride<NoxConfiguration>(c => c.DefinitionFileName, new YamlIgnoreAttribute())
            .WithAttributeOverride<DataSourceConfiguration>(c => c.Id, new YamlIgnoreAttribute())
            .WithAttributeOverride<DataSourceConfiguration>(c => c.DefinitionFileName, new YamlIgnoreAttribute())
            .Build();

        var yaml = serializer.Serialize(service);

        var header = new StringBuilder();
        header.AppendLine($"#");
        header.AppendLine($"# {settings.File}");
        header.AppendLine($"#");
        header.AppendLine($"# yaml-language-server: $schema=https://raw.githubusercontent.com/NoxOrg/Nox/main/src/Nox.Core/Schemas/NoxConfiguration.json");
        header.AppendLine($"#");

        System.IO.File.WriteAllText(settings.File, $"{header}\n{yaml}");

        return 0;
    }


}