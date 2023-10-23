using System.Collections;

namespace Nox.Cli.Variables;

public static class ObjectExtensions
{
    private static int _level;
    private static int _maxLevel;

    /// <summary>
    /// Walks the properties of an object and performs an action on each property.
    /// </summary>
    /// <param name="obj">The object to walk the properties of.</param>
    /// <param name="propertyAction">The action to perform on each property. The action will be passed the full path of the property and its value as arguments.</param>
    /// <param name="path">The current path of the object being walked. This parameter is for internal use and should not be specified when calling the method.</param>
    /// <param name="maxLevel"></param>
    public static void WalkProperties(this object? obj, Action<string, object> propertyAction, string path = "", int maxLevel = 50)
    {
        _level = 0;
        _maxLevel = maxLevel;
        obj.WalkPropertiesInternal(propertyAction, path);
    } 
    
    
    private static void WalkPropertiesInternal(this object? obj, Action<string, object> propertyAction, string path = "")
    {
        _level++;
        if (_level > _maxLevel) return;
        
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
                        value.WalkPropertiesInternal(propertyAction, fullPath);
                    }
                }
            }
        }
        else if (type.IsArray || type.IsEnumerable())
        {
            var enumerable = obj as IEnumerable;
            if (enumerable != null)
            {
                propertyAction(path, obj);

                var index = 0;
                foreach (var item in enumerable)
                {
                    item.WalkPropertiesInternal(propertyAction, $"{path}[{index}]");
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

                WalkPropertiesInternal(propertyValue!, propertyAction, $"{fullPath}");

            }
        }
    }
}
