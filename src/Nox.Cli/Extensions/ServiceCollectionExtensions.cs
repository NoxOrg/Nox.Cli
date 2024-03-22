using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nox.Secrets;
using Nox.Secrets.Abstractions;
using Nox.Solution;

namespace Nox.Cli.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNoxCliServices(this IServiceCollection services, string[] args)
    {
        return services
            .AddSingleton(typeof(NoxSolution), CreateSolution)
            .AddSecretsResolver();
    }
    
    private static NoxSolution CreateSolution(IServiceProvider serviceProvider)
    {
        return new NoxSolutionBuilder()
            .AllowMissingSolutionYaml()
            .Build();
    }
}