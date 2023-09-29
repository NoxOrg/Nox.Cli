using System.Collections.ObjectModel;

namespace Nox.Cli.Abstractions;

public class NoxJobDictionary: KeyedCollection<string, INoxJob>
{
    public NoxJobDictionary(): base(StringComparer.OrdinalIgnoreCase)
    {
        
    }
    
    protected override string GetKeyForItem(INoxJob item)
    {
        return item.Id;
    }
}