using System.Reflection;

namespace Nox.Cli.Abstractions.Helpers;

public class NoxWorkflowContextHelpers
{
    public static Type? ResolveActionProviderTypeFromUses(string uses)
    {
        var actionAssemblyName = $"Nox.Cli.Plugin.{uses.Split('/')[0]}";
        var actionClassNameLower = uses.Replace("/", "").Replace("-", "").Replace("@", "_").ToLower();

        var loadedPaths = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Where(a => a.GetName().Name?.Contains(actionAssemblyName, StringComparison.InvariantCultureIgnoreCase) ?? false)
            .Select(a => a.Location)
            .ToArray();

        var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");

        var toLoad = referencedPaths
            .Where(r => r.Contains(actionAssemblyName, StringComparison.InvariantCultureIgnoreCase))
            .Where(r => !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase))
            .ToArray();

        if (toLoad.Length > 0)
        {
            AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(toLoad[0]));
        }

        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.Contains(actionAssemblyName, StringComparison.InvariantCultureIgnoreCase) ?? false)
            .ToArray();

        Type? actionType = null;
        
        if (assembly.Length > 0)
        {
            actionType = assembly[0].GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.IsAssignableTo(typeof(INoxCliAddin)))
                .Where(t => t.Name.ToLower().Equals(actionClassNameLower))
                .FirstOrDefault();
        }

        return actionType;
    }
}