using Nox.Cli.Abstractions;
using Nox.Cli.Abstractions.Extensions;
using Spectre.Console;

namespace Nox.Cli.Plugin.Console;

public class ConsolePrompt_v1 : INoxCliAddin
{
    public NoxActionMetaData Discover()
    {
        return new NoxActionMetaData
        {
            Name = "console/prompt@v1",
            Author = "Jan Schutte",
            Description = "Gets an input from console",
            RequiresConsole = true,

            Inputs =
            {
                ["prompt"] = new NoxActionInput {
                    Id = "prompt",
                    Description = "The question you would like to ask at the prompt",
                    Default = string.Empty,
                    IsRequired = true
                },
                
                ["response-type"] = new NoxActionInput {
                    Id = "response-type",
                    Description = "The type of response you require: string<default>, integer, boolean",
                    Default = "string",
                    IsRequired = true
                },
                
                ["allow-empty"] = new NoxActionInput {
                    Id = "allow-empty",
                    Description = "Is an empty response allowed?",
                    Default = false,
                    IsRequired = true
                }
            },

            Outputs =
            {
                ["response"] = new NoxActionOutput {
                    Id = "response",
                    Description = "The users responses as a dictionary",
                    Value = string.Empty,
                }
            },
        };
    }

    private string? _prompt;
    private string? _responseType;
    private bool? _allowEmpty;
    
    public Task BeginAsync(IDictionary<string, object> inputs)
    {
        _prompt = inputs.Value<string>("prompt");
        _responseType = inputs.ValueOrDefault<string>("response-type", this);
        _allowEmpty = inputs.Value<bool>("allow-empty");
        return Task.CompletedTask;

    }

    public Task<IDictionary<string, object>> ProcessAsync(INoxWorkflowContext ctx)
    {
        var outputs = new Dictionary<string, object>();

        ctx.SetState(ActionState.Error);

        if (string.IsNullOrEmpty(_prompt) ||
            string.IsNullOrEmpty(_responseType) ||
            _allowEmpty == null)
        {
            ctx.SetErrorMessage("The prompt action was not initialized");
        }
        else 
        {
            try
            {
                switch (_responseType.ToLower())
                {
                    case "boolean":
                        var responseBool = AnsiConsole.Prompt(
                            new TextPrompt<char>(_prompt)
                                .DefaultValueStyle(Style.Parse("mediumpurple3_1"))
                                .ChoicesStyle(Style.Parse("mediumpurple3_1"))
                                .PromptStyle(Style.Parse("seagreen1"))
                                .DefaultValue('y')
                                .AddChoice('y')
                                .AddChoice('n')
                        ) == 'y';

                        outputs["response"] = responseBool;

                        break;

                    case "integer":
                        var promptObjInt = new TextPrompt<int>(_prompt)
                            .PromptStyle(Style.Parse("seagreen1"))
                            .DefaultValueStyle(Style.Parse("mediumpurple3_1"));

                        var responseInt = AnsiConsole.Prompt(promptObjInt);
                                               
                        outputs["response"] = responseInt;
                        
                        break;

                    default:
                        var promptObjString = new TextPrompt<string>(_prompt)
                            .PromptStyle(Style.Parse("seagreen1"))
                            .DefaultValueStyle(Style.Parse("mediumpurple3_1"));

                            
                        promptObjString.AllowEmpty = _allowEmpty.Value;

                        var responseString = AnsiConsole.Prompt(promptObjString);
                        outputs["response"] = responseString;
                        break;
                }
                ctx.SetState(ActionState.Success);
            }
            catch (Exception ex)
            {
                ctx.SetErrorMessage( ex.Message );
            }
        }

        return Task.FromResult<IDictionary<string, object>>(outputs);
    }

    public Task EndAsync()
    {
        return Task.CompletedTask;
    }
}