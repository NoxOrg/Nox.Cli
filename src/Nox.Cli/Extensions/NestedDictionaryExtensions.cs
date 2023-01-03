using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Cli;

public static class NestedDictionaryExtensions
{
    public static void WalkDictionary(this IDictionary<object, object> dictionary, Action<KeyValuePair<string,object>> func, string prefix = "")
    {
        foreach(var (key, value) in dictionary)
        {
            if (value is IDictionary<object, object> subDictionary)
            {
                subDictionary.WalkDictionary(func, $"{prefix}.{key}");
            }
            else if (value is IEnumerable<IDictionary<object, object>> subDictionaryEnumeralbe)
            {
                foreach (var d in subDictionaryEnumeralbe)
                {
                    d.WalkDictionary(func, $"{prefix}.{key}");
                }
            }
            else if (value is IEnumerable<object> subList)
            {
                foreach (var d in subList)
                {
                    if (d is IDictionary<object, object> subListDictionary)
                    {
                        subListDictionary.WalkDictionary(func, $"{prefix}.{key}");
                    }
                    else
                    {
                        func.Invoke(new KeyValuePair<string, object>($"{prefix}.{key}".TrimStart('.'), d));
                    }
                }
            }
            else
            {
                func.Invoke(new KeyValuePair<string, object>($"{prefix}.{key}".TrimStart('.'), value));
            }
        }

    }
}
