
using Nox.Cli.Abstractions.Extensions;
using Nox.Cli.Actions;
using Nox.Cli.Plugin.YamlMaker.JsonSchema;
using RestSharp;
using Spectre.Console;
using System;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nox.Cli.Plugins.YamlMaker;

public class YamlMakerBuild_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "yamlmaker/build@v1",
            Author = "Andre Sharpe",
            Description = "Builds a YAML definition from a defined schema",

            Inputs =
            {
                ["schema-url"] = new NoxActionInput {
                    Id = "schema-url",
                    Description = "The json schema describing the YAML file",
                    Default = string.Empty,
                    IsRequired = true
                },

                ["output-path"] = new NoxActionInput {
                    Id = "output-path",
                    Description = "The folder to save the reulting YAML in",
                    Default = @".\",
                    IsRequired = false
                },

                ["prompt-for"] = new NoxActionInput {
                    Id = "prompt-for",
                    Description = "The verbosity of the promts (all/required/none)",
                    Default = "all",
                    IsRequired = false
                },
            },

            Outputs =
            {
                ["file-path"] = new NoxActionOutput {
                    Id = "file-path",
                    Description = "The full path of the file that was created",
                    Value = string.Empty,
                },
            },
        };
    }
    
    private Uri _schemaUrl = null!;

    private RestClient _client = new();

    private Regex resolveRefs = new("\"\\$ref\\\"\\s*:\\s*\\\"(?<url>[\\w:/\\.]+)\\\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Task BeginAsync(INoxWorkflowContext ctx, IDictionary<string, object> inputs)
    {

        var schemaUrl = inputs.Value<string>("schema-url");

        if (Uri.IsWellFormedUriString(schemaUrl, UriKind.Absolute))
        {
            _schemaUrl = new Uri(schemaUrl);
        };

        return Task.CompletedTask;

    }

    public async Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (_schemaUrl == null)
        {
            ctx.SetErrorMessage("The YamlMaker action was not initialized");
        }
        else
        {
            try
            {
                var json = await ReadSchemaFromUrl(_schemaUrl.ToString());

                if (json != null)
                {
                    var jsonSchema = JsonSerializer.Deserialize<JsonSchema>(json, new JsonSerializerOptions {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (jsonSchema != null)
                    {
                        await AskForProperties(jsonSchema);

                        outputs["file-path"] = AnsiConsole.Ask<string>("File Name?");

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

    private async Task<string?> ReadSchemaFromUrl(string url)
    {
        AnsiConsole.WriteLine($"> Reading {url}...");

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
            var subRef = await ReadSchemaFromUrl(match.Groups["url"].Value);
            if (subRef != null)
            {
                subRef = subRef.Trim();
                subRef = subRef.Substring(1, subRef.Length - 2);
                json = json.Replace(match.Value, subRef);
            }
        }

        return json;
    }

    private async Task AskForProperties(JsonSchema jsonSchema, string indent = "")
    {
        if (!string.IsNullOrWhiteSpace(jsonSchema.Description))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"{indent}[bold mediumpurple3_1]{jsonSchema.Description}[/]");
        }

        indent += "..";

        foreach (var (key,prop) in jsonSchema.Properties)
        {
            if (prop.Type.Equals("object", StringComparison.OrdinalIgnoreCase))
            {
                await AskForProperties(prop, indent);
            }
            else if (prop.Type.Equals("array", StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Items != null)
                {
                    do
                    {
                        await AskForProperties(prop.Items, indent);
                        AnsiConsole.WriteLine();
                    } 
                    while (AnsiConsole.Confirm($"{indent}>>> [bold]Add another[/]?"));
                }
            }
            else 
            {
                var message = prop.Description ?? key;

                AnsiConsole.WriteLine();

                switch (prop.Type.ToLower())
                {
                    case "boolean":
                        prop.UserInput = AnsiConsole.Confirm($"{indent}[bold]{message.EscapeMarkup()}[/]:");
                        break;

                    case "integer":
                        prop.UserInput = AnsiConsole.Prompt(
                            new TextPrompt<int>($"{indent}[bold]{message.EscapeMarkup()}[/]:")
                        );
                        break;

                    default:
                        if (prop.OneOf == null)
                        {
                            prop.UserInput = AnsiConsole.Prompt(
                                new TextPrompt<string>($"{indent}[bold]{message.EscapeMarkup()}[/]:")
                            );
                        }
                        else
                        {
                            prop.UserInput = AnsiConsole.Prompt(
                                new SelectionPrompt<string>()
                                    .Title($"{indent}[bold]{message.EscapeMarkup()}[/]:")
                                    .AddChoices(prop.OneOf.Select(c => c.Const).ToArray())
                            );
                            AnsiConsole.MarkupLine($"{indent}[bold]{message.EscapeMarkup()}[/]: {prop.UserInput}");

                        }
                        break;
                }
            }
        }
    }

}

