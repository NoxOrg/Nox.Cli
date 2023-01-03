
using Microsoft.Extensions.Configuration;
using Nox.Core.Interfaces.Configuration;
using Spectre.Console;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Nox.Cli.Actions;

public class NoxWorkflowExecutor
{
    public async static Task<bool> Execute(string workflowYaml, IConfiguration appConfig, INoxConfiguration noxConfig, IAnsiConsole console)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var configYaml = serializer.Serialize(noxConfig);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var config = deserializer.Deserialize<Dictionary<object, object>>(configYaml);

        var workflow = deserializer.Deserialize<Dictionary<object, object>>(workflowYaml);

        console.WriteLine();
        console.WriteLine($"Validating...");
        console.MarkupLine($"[green3]Workflow: {workflow["name"].ToString().EscapeMarkup()}[/]");

        var ctx = new NoxWorkflowExecutionContext(workflow, config, appConfig);

        List<INoxAction> processedActions = new();

        while (ctx.CurrentAction != null)
        {
            console.WriteLine();
            var message = $"Step {ctx.CurrentAction.Sequence}: {ctx.CurrentAction.Name}";
            console.MarkupLine($"[bold mediumpurple3_1]{message.EscapeMarkup()}[/]");

            var inputs = ctx.GetInputVariables(ctx.CurrentAction);

            if (!ctx.CurrentAction.EvaluateIf())
            {
                console.MarkupLine($"{Emoji.Known.ThumbsUp} Skipped because {ctx.CurrentAction.If.EscapeMarkup()} failed");
                ctx.Next();
                continue;
            }

            await ctx.CurrentAction.BeginAsync(ctx, inputs);

            var outputs = await ctx.CurrentAction.ProcessAsync(ctx);

            ctx.StoreOutputVariables(ctx.CurrentAction, outputs);

            ctx.CurrentAction.EvaluateValidate();

            processedActions.Add(ctx.CurrentAction);

            if (ctx.CurrentAction.State == ActionState.Error)
            {
                if (ctx.CurrentAction.ContinueOnError)
                {
                    console.MarkupLine($"{Emoji.Known.CheckBoxWithCheck} {ctx.CurrentAction.Display.Error.EscapeMarkup()}");
                }
                else
                {
                    ctx.SetErrorMessage(ctx.CurrentAction, ctx.CurrentAction.ErrorMessage);
                    console.MarkupLine($"{Emoji.Known.CryingFace} [bold indianred1]{ctx.CurrentAction.Display.Error.EscapeMarkup()}[/]");
                    break;
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(ctx.CurrentAction.Display.Success))
                {
                    console.MarkupLine($"{Emoji.Known.CheckBoxWithCheck} {ctx.CurrentAction.Display.Success.EscapeMarkup()}");
                }
            }

            ctx.Next();
        }

        await Task.WhenAll( processedActions.Select(p => p.EndAsync(ctx) ) );

        return true;
    }

}




