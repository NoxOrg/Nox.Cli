using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Abstractions.Extensions;
using RestSharp;
using Spectre.Console;

namespace Nox.Cli.Plugin.Console;

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

    private IDictionary<string, string?>? _schemaCache;

    private string[]? _includedPrompts;

    private string[]? _excludedPrompts;
    
    private Dictionary<string,object>? _defaults = null;

    private readonly StringBuilder _sbYaml = new();

    private Dictionary<string,string>? _fileOptions = null;

    private readonly RestClient _client = new();

    private readonly Regex resolveRefs = new("\"\\$ref\\\"\\s*:\\s*\\\"(?<url>[\\w:/\\.]+)\\\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Dictionary<string, object> _responses = new();


    public Task BeginAsync(IDictionary<string, object> inputs)
    {

        var schemaUrl = inputs.Value<string>("schema-url");

        if (Uri.IsWellFormedUriString(schemaUrl, UriKind.Absolute))
        {
            _schemaUrl = (new Uri(schemaUrl)).AbsoluteUri;
        }

        _schema = inputs.Value<string>("schema");

        _includedPrompts = inputs.Value<string[]>("include-prompts");


        _excludedPrompts = inputs.Value<string[]>("exclude-prompts");

        _defaults = inputs.Value<Dictionary<string,object>>("defaults");

        //_fileOptions = inputs.Value<Dictionary<string, string>>("output-file");
        _fileOptions = inputs.Value<Dictionary<string, string>>("output-file");

        return Task.CompletedTask;

    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {

        if (ctx.IsServer) throw new NoxCliException("This action cannot be executed on a server. remove the run-at-server attribute for this step in your Nox workflow.");
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
                var json = _schema;

                if (string.IsNullOrWhiteSpace(json))
                {
                    var baseUrl = "";
                    var schemaName = "";
                    var lastIndex = _schemaUrl!.LastIndexOf('/');
                    if (lastIndex != -1)
                    {
                        baseUrl = _schemaUrl![..lastIndex];
                        schemaName = _schemaUrl!.Substring(lastIndex + 1);
                    }
                    json = await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Clock)
                        .StartAsync("Reading schemas...", fn =>
                            ReadSchemaFromUrl(baseUrl, schemaName, fn)
                        );                    
                }
                
                if (json != null)
                {
                    await File.WriteAllTextAsync("/home/jan/Downloads/schema.json", json);

                    var serializeOptions = new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    serializeOptions.Converters.Add(new JsonSchemaTypeConverter());
                    
                    var jsonSchema = JsonSerializer.Deserialize<JsonSchema>(json, serializeOptions);

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

                            var outputFilePath = _fileOptions["filename"];
                            if (_fileOptions.TryGetValue("folder", out var folder))
                            {
                                outputFilePath = Path.Combine(folder, outputFilePath);
                            }
                            
                            File.WriteAllText(outputFilePath,contents);
                            outputs["file-path"] = outputFilePath;

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

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }

    private async Task<string?> ReadSchemaFromUrl(string baseUrl, string schemaName, StatusContext ctx, bool isRecursiveCall = false)
    {
        ctx.Status = $"Reading schema {schemaName}...";

        if (!isRecursiveCall) _schemaCache = new Dictionary<string, string?>();

        var json = "";

        if (!_schemaCache!.ContainsKey(schemaName))
        {

            var request = new RestRequest($"{baseUrl}/{schemaName}") { Method = Method.Get };

            request.AddHeader("Accept", "application/json");

            var onlineFilesJson = await _client.ExecuteAsync(request);

            json = onlineFilesJson.Content;

            _schemaCache![schemaName] = json;

            if (json == null)
            {
                return null;
            }

            var matches = resolveRefs.Matches(json);

            foreach (Match match in matches)
            {
                var subRef = await ReadSchemaFromUrl(baseUrl, match.Groups["url"].Value, ctx, true);
                if (!string.IsNullOrWhiteSpace(subRef))
                {
                    subRef = subRef.Trim();
                    subRef = subRef.Substring(1, subRef.Length - 2);
                    json = json.Replace(match.Value, subRef);
                }
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

        if (jsonSchema.AnyOf != null)
        {
            foreach (var item in jsonSchema.AnyOf)
            {
                if (item.Properties is { Count: 1 } && item.Properties.ContainsKey("$ref")) continue;
                await AskForProperties(item, indent, fullKey);
            }
        }
        if (jsonSchema.Properties != null)
        {
            foreach (var (key, prop) in jsonSchema.Properties)
            {
                var newFullKey = $"{fullKey}.{key}".TrimStart('.');

                if (_includedPrompts != null && !_includedPrompts.Any(f => newFullKey.StartsWith(f, StringComparison.OrdinalIgnoreCase)))
                {
                    if (_defaults != null && _defaults.Any(d => newFullKey.Equals(d.Key, StringComparison.OrdinalIgnoreCase)))
                    {
                        _sbYaml.AppendLine($"{yamlSpacing}{yamlSpacingPostfix}{key}: {_defaults[newFullKey]}");
                        _responses[newFullKey] = _defaults[newFullKey];
                    }

                    continue;
                }

                if (_excludedPrompts != null && _excludedPrompts.Any(f => newFullKey.StartsWith(f, StringComparison.OrdinalIgnoreCase)))
                {
                    if (_defaults != null && _defaults.Any(d => newFullKey.Equals(d.Key, StringComparison.OrdinalIgnoreCase)))
                    {
                        _sbYaml.AppendLine($"{yamlSpacing}{yamlSpacingPostfix}{key}: {_defaults[newFullKey]}");
                        _responses[newFullKey] = _defaults[newFullKey];
                    }

                    continue;
                }

                if (prop.AnyOf != null)
                {
                    foreach (var item in prop.AnyOf)
                    {
                        if (item.Properties is { Count: 1 } && item.Properties.ContainsKey("$ref")) continue;
                        await AskForProperties(item, indent, newFullKey);
                    }
                }
                else if (prop.Type != null)
                {
                    if (prop.Type!.TypeName!.Equals("object", StringComparison.OrdinalIgnoreCase))
                    {
                        _sbYaml.AppendLine($"{yamlSpacing}{key}:");
                        await AskForProperties(prop, indent, newFullKey);
                    }

                    else if (prop.Type!.TypeName!.Equals("array", StringComparison.OrdinalIgnoreCase))
                    {
                        var index = 0;
                        if (prop.Items != null)
                        {
                            _sbYaml.AppendLine($"{yamlSpacing}{key}:");

                            do
                            {
                                indent += "..";
                                await AskForProperties(prop.Items, indent, $"{newFullKey}[{index}]");   
                                AnsiConsole.WriteLine();
                                index++;
                            } while (
                                AnsiConsole.Prompt(
                                    new TextPrompt<char>($"[grey]{new string('.', 40)}[/] [bold]Add another[/]?")
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
                        var prefix = $"[grey]{newFullKey.PadRight(40, '.').EscapeMarkup()}[/] ";
                        var message = (prop.Description ?? newFullKey).EscapeMarkup();
                        var prompt = $"{prefix}[bold]{message}[/]:";

                        AnsiConsole.WriteLine();

                        switch (prop.Type.TypeName.ToLower())
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
                                if (prop.AnyOf == null)
                                {
                                    var promptObjString = new TextPrompt<string>(prompt)
                                        .PromptStyle(Style.Parse("seagreen1"))
                                        .DefaultValueStyle(Style.Parse("mediumpurple3_1"));


                                    promptObjString.AllowEmpty = jsonSchema.Required != null && !jsonSchema.Required.Contains(key);

                                    var defaultValueString = GetDefaultString(prop, newFullKey);

                                    if (!string.IsNullOrWhiteSpace(defaultValueString))
                                    {
                                        promptObjString.DefaultValue(defaultValueString);
                                    }

                                    var responseString = AnsiConsole.Prompt(promptObjString);
                                    if (!string.IsNullOrEmpty(responseString))
                                    {
                                        _sbYaml.AppendLine($"{yamlSpacing}{yamlSpacingPostfix}{key}: {responseString}");
                                        _responses[newFullKey] = responseString;
                                    }
                                }
                                else
                                {
                                    var responseChoice = AnsiConsole.Prompt(
                                        new SelectionPrompt<string>()
                                            .Title(prompt)
                                            .HighlightStyle(Style.Parse("mediumpurple3_1"))
                                            .AddChoices(prop.Enum!.ToArray())
                                    );
                                    
                                    _responses[newFullKey] = responseChoice;
                                    _sbYaml.AppendLine($"{yamlSpacing}{yamlSpacingPostfix}{key}: {responseChoice}");
                                    AnsiConsole.MarkupLine($"{prompt} [seagreen1]{_responses[newFullKey]}[/]");

                                }

                                break;
                        }
                    }
                } 

                if (fullKey.EndsWith(']'))
                {
                    yamlSpacingPostfix = "  ";
                }

            } 
        }
        if (jsonSchema.Enum != null)
        {
            var prefix = $"[grey]{fullKey.PadRight(45, '.').EscapeMarkup()}[/] ";
            var message = (jsonSchema.Description ?? fullKey).EscapeMarkup();
            var prompt = $"{prefix}[bold]{message}[/]:";
            var responseChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(prompt)
                    .HighlightStyle(Style.Parse("mediumpurple3_1"))
                    .AddChoices(jsonSchema.Enum!.ToArray())
            );
                                    
            _responses[fullKey] = responseChoice;
            _sbYaml.AppendLine($"{yamlSpacing}{yamlSpacingPostfix}{fullKey}: {responseChoice}");
            AnsiConsole.MarkupLine($"{prompt} [seagreen1]{_responses[fullKey]}[/]");
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
            if (prop.Default != null && !string.IsNullOrWhiteSpace(prop.Default.ToString()))
            {
                defaultValue = prop.Default.ToString();
            } else if (!string.IsNullOrWhiteSpace(prop.Type!.Default?.ToString()))
            {
                defaultValue = prop.Type.Default.ToString();
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
            if (!string.IsNullOrWhiteSpace(prop.Type!.Default?.ToString()))
            {
                defaultValue = int.Parse(prop.Type.Default.ToString() ?? "0");
            }
        }
        return defaultValue ?? 0;
    }

}

