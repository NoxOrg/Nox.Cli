
using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.Actions;
using Nox.Cli.Plugin.YamlMaker.JsonSchema;
using RestSharp;
using Spectre.Console;
using System;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nox.Cli.Plugins.Console;

public class ConsolePromptSchema_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "console/prompt-schema@v1",
            Author = "Andre Sharpe",
            Description = "Gets input from console based on a JSON schema",
            RequiresConsole = true,

            Inputs =
            {
                ["schema-url"] = new NoxActionInput {
                    Id = "schema-url",
                    Description = "The json schema describing the questions",
                    Default = string.Empty,
                    IsRequired = true
                },

                ["output-file"] = new NoxActionInput {
                    Id = "output-file",
                    Description = "The file info to save the user's responses into",
                    Default = new Dictionary<string,object>(),
                    IsRequired = false
                },

                ["include-prompts"] = new NoxActionInput {
                    Id = "include-prompts",
                    Description = "The properties to include prompts for",
                    Default = new string[] {},
                    IsRequired = false
                },

                ["exclude-prompts"] = new NoxActionInput {
                    Id = "exclude-prompts",
                    Description = "The properties to not prompts for",
                    Default = new string[] {},
                    IsRequired = false
                },

                ["defaults"] = new NoxActionInput {
                    Id = "defaults",
                    Description = "The default values for properties",
                    Default = new Dictionary<string,object>(),
                    IsRequired = false
                },

            },

            Outputs =
            {
                ["response"] = new NoxActionOutput {
                    Id = "response",
                    Description = "The users responses as a dictionary",
                    Value = string.Empty,
                },                
                ["file-path"] = new NoxActionOutput {
                    Id = "file-path",
                    Description = "The full path of the file that was created",
                    Value = string.Empty,
                },
            },
        };
    }
    
    private string? _schemaUrl = null!;

    private string? _schema = null!;

    private string[]? _includedProperties;

    private string[]? _excludedProperties;
    
    private Dictionary<string,object>? _defaults = null;

    private readonly StringBuilder _sbYaml = new();

    private Dictionary<string,string>? _fileOptions = null;

    private readonly RestClient _client = new();

    private readonly Regex resolveRefs = new("\"\\$ref\\\"\\s*:\\s*\\\"(?<url>[\\w:/\\.]+)\\\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Dictionary<string, object> _responses = new();


    public Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string, object> inputs)
    {

        var schemaUrl = inputs.Value<string>("schema-url");

        if (Uri.IsWellFormedUriString(schemaUrl, UriKind.Absolute))
        {
            _schemaUrl = (new Uri(schemaUrl)).AbsoluteUri;
        };

        _schema = inputs.Value<string>("schema");

        _includedProperties = inputs.Value<string[]>("include-prompts");

        _excludedProperties = inputs.Value<string[]>("exclude-prompts");

        _defaults = inputs.Value<Dictionary<string,object>>("defaults");

        _fileOptions = inputs.Value<Dictionary<string, string>>("output-file");

        return Task.CompletedTask;

    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_schemaUrl == null && _schema == null)
        {
            ctx.SetErrorMessage("The prompt using schema action was not initialized");
        }
        else 
        {
            try
            {

                var json = _schema ?? await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Clock)
                    .StartAsync("Reading schemas...", ctx =>
                        ReadSchemaFromUrl(_schemaUrl!, ctx)
                    );

                if (json != null)
                {
                    var jsonSchema = JsonSerializer.Deserialize<JsonSchema>(json, new JsonSerializerOptions {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (jsonSchema != null)
                    {
                        if (_schemaUrl != null)
                        {
                            _sbYaml.AppendLine($"#");
                            _sbYaml.AppendLine($"# yaml-language-server: $schema={_schemaUrl}");
                        }
                        _sbYaml.AppendLine($"#");
                        _sbYaml.AppendLine($"");

                        await AskForProperties(jsonSchema);

                        foreach(var (key,value) in _responses)
                        {
                            outputs[key] = value;
                        }

                        if (_fileOptions != null && _fileOptions.ContainsKey("filename"))
                        {
                            _sbYaml.Insert(0,$"# {Path.GetFileName(_fileOptions["filename"])}{Environment.NewLine}");
                            _sbYaml.Insert(0,$"#{Environment.NewLine}");

                            var contents = _sbYaml.ToString();

                            File.WriteAllText(_fileOptions["filename"],contents);

                            AnsiConsole.WriteLine();
                            AnsiConsole.MarkupLine($"[bold mediumpurple3_1]Created {_fileOptions["filename"].EscapeMarkup()}[/]");
                        }

                        ctx.SetState(ActionState.Success);
                    }
                }
            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage( ex.Message );
            }
        }

        return outputs;
    }

    public Task EndAsync(INoxWorkflowContext ctx)
    {
        return Task.CompletedTask;
    }

    private async Task<string?> ReadSchemaFromUrl(string url, StatusContext ctx)
    {
        ctx.Status = $"Reading schema {url}...";

        var request = new RestRequest(url) { Method = Method.Get };

        request.AddHeader("Accept", "application/json");

        var onlineFilesJson = await _client.ExecuteAsync(request);

        var json = onlineFilesJson.Content;

        if (json == null)
        {
            return null;
        }

        var matches = resolveRefs.Matches(json);

        foreach(Match match in matches) 
        {
            var subRef = await ReadSchemaFromUrl(match.Groups["url"].Value, ctx);
            if (subRef != null)
            {
                subRef = subRef.Trim();
                subRef = subRef.Substring(1, subRef.Length - 2);
                json = json.Replace(match.Value, subRef);
            }
        }

        return json;
    }

    private async Task AskForProperties(JsonSchema jsonSchema, string indent = "", string fullKey = "")
    {
        if (!string.IsNullOrWhiteSpace(jsonSchema.Description))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[yellow]{jsonSchema.Description.EscapeMarkup()}[/]");
        }

        var yamlSpacing = indent.Replace('.', ' ');
        var yamlSpacingPostfix = "";

        if (fullKey.EndsWith(']'))
        {
            yamlSpacingPostfix = "- ";
        }

        indent += "..";

        foreach (var (key,prop) in jsonSchema.Properties)
        {
            var newFullKey = $"{fullKey}.{key}".TrimStart('.');

            if (_includedProperties != null && !_includedProperties.Any(f => newFullKey.StartsWith(f,StringComparison.OrdinalIgnoreCase)))
            {
                if (_defaults != null && _defaults.Any(d => newFullKey.Equals(d.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    _sbYaml.AppendLine($"{yamlSpacing}{yamlSpacingPostfix}{key}: {_defaults[newFullKey]}");
                    _responses[newFullKey] = _defaults[newFullKey];
                }
                continue;
            }

            if (_excludedProperties != null && _excludedProperties.Any(f => newFullKey.StartsWith(f, StringComparison.OrdinalIgnoreCase)))
            {
                if (_defaults != null && _defaults.Any(d => newFullKey.Equals(d.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    _sbYaml.AppendLine($"{yamlSpacing}{yamlSpacingPostfix}{key}: {_defaults[newFullKey]}");
                    _responses[newFullKey] = _defaults[newFullKey];
                }
                continue;
            }

            if (prop.Type.Equals("object", StringComparison.OrdinalIgnoreCase))
            {
                _sbYaml.AppendLine($"{yamlSpacing}{key}:");
                await AskForProperties(prop, indent, newFullKey);
            }

            else if (prop.Type.Equals("array", StringComparison.OrdinalIgnoreCase))
            {
                var index = 0;
                if (prop.Items != null)
                {
                    _sbYaml.AppendLine($"{yamlSpacing}{key}:");

                    do
                    {
                        await AskForProperties(prop.Items, indent, $"{newFullKey}[{index}]");
                        AnsiConsole.WriteLine();
                        index++;
                    }
                    while (
                        AnsiConsole.Prompt(
                            new TextPrompt<char>($"[grey]{new string('.',30)}[/] [bold]Add another[/]?")
                                .DefaultValueStyle(Style.Parse("mediumpurple3_1"))
                                .ChoicesStyle(Style.Parse("mediumpurple3_1"))
                                .PromptStyle(Style.Parse("seagreen1"))
                                .DefaultValue('n')
                                .AddChoice('y')
                                .AddChoice('n')
                    ) == 'y');
                }
            }
            else 
            {
                var prefix = $"[grey]{newFullKey.PadRight(30, '.').EscapeMarkup()}[/] ";
                var message = (prop.Description ?? newFullKey).EscapeMarkup();
                var prompt = $"{prefix}[bold]{message}[/]:";

                AnsiConsole.WriteLine();

                switch (prop.Type.ToLower())
                {
                    case "boolean":
                        var responseBool = AnsiConsole.Prompt(
                            new TextPrompt<char>(prompt)
                                .DefaultValueStyle(Style.Parse("mediumpurple3_1"))
                                .ChoicesStyle(Style.Parse("mediumpurple3_1"))
                                .PromptStyle(Style.Parse("seagreen1"))
                                .DefaultValue('y')
                                .AddChoice('y')
                                .AddChoice('n')
                        ) == 'y';

                        _sbYaml.AppendLine($"{yamlSpacing}{yamlSpacingPostfix}{key}: {responseBool.ToString().ToLower()}");
                        _responses[newFullKey] = responseBool;

                        break;

                    case "integer":
                        var promptObjInt = new TextPrompt<int>(prompt)
                            .PromptStyle(Style.Parse("seagreen1"))
                            .DefaultValueStyle(Style.Parse("mediumpurple3_1"));

                        var defaultValueInt = GetDefaultInt(prop, newFullKey);

                        if (defaultValueInt != 0)
                        {
                            promptObjInt.DefaultValue(defaultValueInt);
                        }
                        var responseInt = AnsiConsole.Prompt(promptObjInt);

                        _sbYaml.AppendLine($"{yamlSpacing}{yamlSpacingPostfix}{key}: {responseInt}");
                        _responses[newFullKey] = responseInt;

                        break;

                    default:
                        if (prop.OneOf == null)
                        {
                            var promptObjString = new TextPrompt<string>(prompt)
                                .PromptStyle(Style.Parse("seagreen1"))
                                .DefaultValueStyle(Style.Parse("mediumpurple3_1"));

                            promptObjString.AllowEmpty = false;

                            var defaultValueString = GetDefaultString(prop, newFullKey);

                            if (!string.IsNullOrWhiteSpace(defaultValueString))
                            {
                                promptObjString.DefaultValue(defaultValueString);
                            }
                            
                            var responseString = AnsiConsole.Prompt(promptObjString);
                            _sbYaml.AppendLine($"{yamlSpacing}{yamlSpacingPostfix}{key}: {responseString}");
                            _responses[newFullKey] = responseString;
                        }
                        else
                        {
                            var responseChoice = AnsiConsole.Prompt(
                                new SelectionPrompt<string>()
                                    .Title(prompt)
                                    .HighlightStyle(Style.Parse("mediumpurple3_1"))
                                    .AddChoices(prop.OneOf.Select(c => c.Const).ToArray())
                            );

                            _responses[newFullKey] = responseChoice;
                            _sbYaml.AppendLine($"{yamlSpacing}{yamlSpacingPostfix}{key}: {responseChoice}");
                            AnsiConsole.MarkupLine($"{prompt} [seagreen1]{_responses[newFullKey]}[/]");

                        }
                        break;
                }
            }

            if (fullKey.EndsWith(']'))
            {
                yamlSpacingPostfix = "  ";
            }

        }
    }

    private string GetDefaultString(JsonSchema prop, string key)
    {
        string? defaultValue = null;
        
        if (_defaults?.ContainsKey(key) ?? false)
        {
            defaultValue = (string)_defaults[key];
        }

        if (string.IsNullOrWhiteSpace(defaultValue))
        {
            if (!string.IsNullOrWhiteSpace(prop.Default?.ToString()))
            {
                defaultValue = prop.Default.ToString();
            }
        }
        return defaultValue ?? string.Empty;
    }

    private int GetDefaultInt(JsonSchema prop, string key)
    {
        int? defaultValue = null;

        if (_defaults?.ContainsKey(key) ?? false)
        {
            defaultValue = int.Parse(_defaults[key]?.ToString() ?? "0");
        }

        if (defaultValue == 0)
        {
            if (!string.IsNullOrWhiteSpace(prop.Default?.ToString()))
            {
                defaultValue = int.Parse(prop.Default.ToString() ?? "0");
            }
        }
        return defaultValue ?? 0;
    }

}

