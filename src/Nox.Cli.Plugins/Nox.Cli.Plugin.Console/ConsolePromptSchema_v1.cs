using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Exceptions;
using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.Plugin.Console.JsonSchema;
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

    private readonly Regex _resolveRefs = new("\"\\$ref\\\"\\s*:\\s*\\\"(?<url>[\\w:/\\.]+)\\\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Dictionary<string, object> _responses = new();

    private bool _isArrayStart = false;

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
                    var serializeOptions = new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    serializeOptions.Converters.Add(new JsonSchemaTypeConverter());
                    
                    var jsonSchema = JsonSerializer.Deserialize<JsonSchema.JsonSchema>(json, serializeOptions);

                    if (jsonSchema != null)
                    {
                        if (_schemaUrl != null)
                        {
                            _sbYaml.AppendLine($"#");
                            _sbYaml.AppendLine($"# yaml-language-server: $schema={_schemaUrl}");
                        }
                        _sbYaml.AppendLine($"#");
                        _sbYaml.AppendLine($"");

                        await ProcessSchema(jsonSchema);

                        foreach(var (key,value) in _responses)
                        {
                            outputs[key] = value;
                        }


                        if (_fileOptions != null && _fileOptions.ContainsKey("filename"))
                        {
                            _sbYaml.Insert(0,$"# {Path.GetFileName(_fileOptions["filename"])}{Environment.NewLine}");
                            _sbYaml.Insert(0,$"#{Environment.NewLine}");

                            var contents = _sbYaml.ToString();

                            //await File.WriteAllTextAsync("/home/jan/Downloads/solution.yaml", contents);

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

            var matches = _resolveRefs.Matches(json);

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

    /// <summary>
    /// This method is recursive through the schema, it is quite complicated, proceed with caution.
    /// </summary>
    /// <param name="schema">The input schema</param>
    /// <param name="rootKey">the root path of the current key</param>
    /// <param name="key">The current key to process</param>
    private async Task ProcessSchema(JsonSchema.JsonSchema schema, string rootKey = "", string key = "")
    {
        var newKey = $"{rootKey}.{key}".TrimStart('.');
        var prefix = $"[grey]{newKey.PadRight(40, '.').EscapeMarkup()}[/] ";
        var yamlSpacing = new string(' ', newKey.Count(d => d == '.') * 2);
        
        var yamlSpacingPostfix = "";

        if (_isArrayStart)
        {
            yamlSpacingPostfix = "- ";
        }
        else
        {
            if (rootKey.EndsWith(']')) yamlSpacing += "  ";    
        }

        if (!string.IsNullOrWhiteSpace(key) && _includedPrompts != null && !_includedPrompts.Any(f => newKey.StartsWith(f, StringComparison.OrdinalIgnoreCase)))
        {
            if (_defaults != null && _defaults.Any(d => key.Equals(d.Key, StringComparison.OrdinalIgnoreCase)))
            {
                _sbYaml.AppendLine($"{key}: {_defaults[key]}");
                _responses[newKey] = _defaults[newKey];
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(key) && _excludedPrompts != null && _excludedPrompts.Any(f => newKey.StartsWith(f, StringComparison.OrdinalIgnoreCase)))
        {
            if (_defaults != null && _defaults.Any(d => newKey.Equals(d.Key, StringComparison.OrdinalIgnoreCase)))
            {
                _sbYaml.AppendLine($"{key}: {_defaults[newKey]}");
                _responses[prefix] = _defaults[newKey];
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(schema.Description))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[yellow]{schema.Description.EscapeMarkup()}[/]");
        }

        var message = (schema.Description ?? newKey).EscapeMarkup();
        var prompt = $"{prefix}[bold]{message}[/]:";
        var isRequired = schema.Required != null && schema.Required.Contains(newKey);

        if (schema.JsonSchemaType == null)
        {
            if (schema.AnyOf != null)
            {
                await ProcessSchema(schema.AnyOf[0], rootKey, key);
            }
        }
        else
        {
            switch (schema.JsonSchemaType.Type)
            {
                case SchemaType.Boolean:
                    _isArrayStart = false;
                    PromptBoolean(prompt, rootKey, key, yamlSpacing + yamlSpacingPostfix);
                    break;
                case SchemaType.Integer:
                    _isArrayStart = false;
                    PromptInteger(prompt, rootKey, key, yamlSpacing + yamlSpacingPostfix, isRequired);
                    break;
                case SchemaType.String:
                    _isArrayStart = false;
                    if (schema.Enum != null)
                    {
                        PromptEnum(prompt, rootKey, key, yamlSpacing + yamlSpacingPostfix, schema.Enum);                        
                    }
                    else
                    {
                        PromptString(prompt, rootKey, key, yamlSpacing + yamlSpacingPostfix, isRequired);    
                    }
                    break;
                case SchemaType.Object:
                    if (!key.EndsWith(']'))
                    {
                        _sbYaml.AppendLine();
                        AppendKey(yamlSpacing, key);
                    }
                    foreach (var prop in schema.Properties!)
                    {
                        await ProcessSchema(prop.Value, newKey, prop.Key);
                    }
                    break;
                case SchemaType.Array:
                    _isArrayStart = false;
                    if (schema.Items != null)
                    {
                        if (schema.Items.AnyOf != null)
                        {
                            _sbYaml.AppendLine();
                            AppendKey(yamlSpacing, key);
                            
                            var index = 0;
                            do
                            {
                                _isArrayStart = true;
                                await ProcessSchema(schema.Items.AnyOf[0], rootKey, $"{key}[{index}]");
                                _sbYaml.AppendLine();
                                AnsiConsole.WriteLine();
                                index++;
                            } while (
                                AnsiConsole.Prompt(
                                    new TextPrompt<char>($"[grey]{yamlSpacing}[/] [bold]Add another[/]?")
                                        .DefaultValueStyle(Style.Parse("mediumpurple3_1"))
                                        .ChoicesStyle(Style.Parse("mediumpurple3_1"))
                                        .PromptStyle(Style.Parse("seagreen1"))
                                        .DefaultValue('n')
                                        .AddChoice('y')
                                        .AddChoice('n')
                                ) == 'y');    
                        } else if (schema.Items.Enum != null)
                        {
                            schema.Items.JsonSchemaType!.Type = SchemaType.EnumList;
                            await ProcessSchema(schema.Items, rootKey, key);
                        }
                    }
                    break;
                case SchemaType.EnumList:
                    _isArrayStart = false;
                    PromptMultipleEnum(prompt, rootKey, key, yamlSpacing + yamlSpacingPostfix, schema.Enum!);    
                    break;
            }
        }
    }
    
    private void PromptBoolean(string prompt, string rootKey, string key, string yamlPrefix)
    {
        var newKey = $"{rootKey}.{key}".TrimStart('.');
        var response = AnsiConsole.Prompt(
            new TextPrompt<char>(prompt)
                .DefaultValueStyle(Style.Parse("mediumpurple3_1"))
                .ChoicesStyle(Style.Parse("mediumpurple3_1"))
                .PromptStyle(Style.Parse("seagreen1"))
                .DefaultValue('y')
                .AddChoice('y')
                .AddChoice('n')
        ) == 'y';

        _sbYaml.AppendLine($"{yamlPrefix}{key}: {response.ToString().ToLower()}");
        _responses[newKey] = response;
    }
    
    private void PromptInteger(string prompt, string rootKey, string key, string yamlPrefix, bool isRequired)
    {
        var newKey = $"{rootKey}.{key}".TrimStart('.');
        var spectrePrompt = new TextPrompt<int>(prompt)
            .PromptStyle(Style.Parse("seagreen1"))
            .DefaultValueStyle(Style.Parse("mediumpurple3_1"));

        spectrePrompt.AllowEmpty = !isRequired;

        var defaultValue = GetDefault<int>(newKey);

        if (defaultValue != 0)
        {
            spectrePrompt.DefaultValue(defaultValue);
        }

        var response = AnsiConsole.Prompt(spectrePrompt);

        _sbYaml.AppendLine($"{yamlPrefix}{key}: {response}");
        _responses[newKey] = response;
    }

    private void PromptString(string prompt, string rootKey, string key, string yamlPrefix, bool isRequired)
    {
        var newKey = $"{rootKey}.{key}".TrimStart('.');
        var spectrePrompt = new TextPrompt<string>(prompt)
            .PromptStyle(Style.Parse("seagreen1"))
            .DefaultValueStyle(Style.Parse("mediumpurple3_1"));

        spectrePrompt.AllowEmpty = !isRequired;

        var defaultValue = GetDefault<string>(newKey);

        if (!string.IsNullOrWhiteSpace(defaultValue))
        {
            spectrePrompt.DefaultValue(defaultValue);
        }

        var response = AnsiConsole.Prompt(spectrePrompt);
        if (!string.IsNullOrEmpty(response))
        {
            _sbYaml.AppendLine($"{yamlPrefix}{key}: {response}");
            _responses[newKey] = response;
        }
    }

    private void PromptEnum(string prompt, string rootKey, string key, string yamlPrefix, List<string> enumList)
    {
        var newKey = $"{rootKey}.{key}".TrimStart('.');
        var response = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(prompt)
                .HighlightStyle(Style.Parse("mediumpurple3_1"))
                .AddChoices(enumList.ToArray())
        );
                                    
        _responses[newKey] = response;
        _sbYaml.AppendLine($"{yamlPrefix}{key}: {response}");
        AnsiConsole.MarkupLine($"{prompt} [seagreen1]{_responses[newKey]}[/]");
    }
    
    private void PromptMultipleEnum(string prompt, string rootKey, string key, string yamlPrefix, List<string> enumList)
    {
        var newKey = $"{rootKey}.{key}".TrimStart('.');
        var response = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title(prompt)
                .HighlightStyle(Style.Parse("mediumpurple3_1"))
                .AddChoices(enumList.ToArray())
        );

        var responseValue = String.Join(',', response);
        _responses[newKey] = responseValue; 
        _sbYaml.AppendLine($"{yamlPrefix}{key}: [{responseValue}]");
        AnsiConsole.MarkupLine($"{prompt} [seagreen1]{_responses[newKey]}[/]");
    }

    private void AppendKey(string yamlSpacing, string key)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            _sbYaml.AppendLine($"{yamlSpacing}{key}:");    
        }
    }
    
    
    private T? GetDefault<T>(string key)
    {
        if (_defaults?.ContainsKey(key) ?? false)
        {
            return (T)_defaults[key];
        }

        return default;
    }
    
}

