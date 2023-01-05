using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Cli;

public static class ObjectExtensions
{
    public static void WalkProperties(this object obj, Action<string, object> propertyAction, string path = "")
    {
        if (obj == null)
        {
            propertyAction(path, null!);
            return;
        }

        var type = obj.GetType();

        if (type.IsSimpleType())
        {
            propertyAction(path, obj);
        }
        else if (type.IsDictionary())
        {
            var dictionary = obj as IDictionary;
            if (dictionary != null)
            {
                foreach (var key in dictionary.Keys)
                {
                    var value = dictionary[key];
                    var fullPath = string.IsNullOrEmpty(path) ? $"[{key}]" : $"{path}[{key}]";
                    if (value == null)
                    {
                        propertyAction(fullPath, null!);
                    }
                    else
                    {
                        value.WalkProperties(propertyAction, fullPath);
                    }
                }
            }
        }
        else if (type.IsArray || type.IsEnumerable())
        {
            var enumerable = obj as IEnumerable;
            if (enumerable != null)
            {
                var index = 0;
                foreach (var item in enumerable)
                {
                    item.WalkProperties(propertyAction, $"{path}[{index}]");
                    index++;
                }
            }
        }
        else
        {
            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                var propertyName = property.Name;
                var propertyValue = property.GetValue(obj);
                var fullPath = string.IsNullOrEmpty(path) ? propertyName : $"{path}.{propertyName}";

                WalkProperties(propertyValue!, propertyAction, $"{fullPath}");

            }
        }
    }
}
