using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Cli;

public static class NestedDictionaryExtensions
{
    public static void WalkDictionary(this IDictionary<object, object> dictionary, Action<KeyValuePair<string, object>> func, string prefix = "")
    {
        foreach (var (key, value) in dictionary)
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
                var i = 0;
                foreach (var d in subList)
                {
                    if (d is IDictionary<object, object> subListDictionary)
                    {
                        subListDictionary.WalkDictionary(func, $"{prefix}.{key}[{i++}]");
                    }
                    else
                    {
                        func.Invoke(new KeyValuePair<string, object>($"{prefix}.{key}[{i++}]".TrimStart('.'), d));
                    }
                }
            }
            else
            {
                func.Invoke(new KeyValuePair<string, object>($"{prefix}.{key}".TrimStart('.'), value));
            }
        }

    }

    public static void WalkObjectProperties(this object? obj, Action<KeyValuePair<string, object?>> func, string prefix = "")
    {
        if (obj == null) return;
        
        Type objType = obj.GetType();

        if (objType.IsAssignableTo(typeof(IList)))
        {
            var i = 0;
            foreach (var item in (IEnumerable)obj)
            {
                if (item.GetType().IsSimpleType())
                {
                    func.Invoke(new KeyValuePair<string, object?>($"{prefix}[{i++}]".TrimStart('.'), item));
                }
                else
                {
                    WalkObjectProperties(item, func, $"{prefix}[{i++}]");
                }
            }
            return;
        }

        var properties = objType.GetProperties();

        foreach (var property in properties)
        {
            var propValue = property.GetValue(obj);

            if (property.PropertyType.IsAssignableTo(typeof(IList)))
            {
                var i = 0;
                var list = (IList?)propValue;
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        WalkObjectProperties(item, func, $"{prefix}.{property.Name}[{i++}]");
                    }
                }
            }
            else if (property.PropertyType.IsAssignableTo(typeof(IDictionary)))
            {
                dynamic dict = propValue!;
                if (dict != null)
                {
                    foreach (var kv in dict)
                    {
                        var t = (Type)kv.Value.GetType();

                        if (t.IsSimpleType())
                        {
                            func.Invoke(new KeyValuePair<string, object?>($"{prefix}.{property.Name}.{kv.Key}".TrimStart('.'), kv.Value));
                        }
                        else
                        {
                            WalkObjectProperties(kv.Value, func, $"{prefix}.{property.Name}.{kv.Key}");
                        }
                    }
                }
            }
            else
            {
                if (property.PropertyType.Assembly == objType.Assembly)
                {
                    WalkObjectProperties(propValue, func, $"{prefix}.{property.Name}");
                }
                else
                {
                    func.Invoke(new KeyValuePair<string, object?>($"{prefix}.{property.Name}".TrimStart('.'), propValue));
                }
            }
        }
    }

}
