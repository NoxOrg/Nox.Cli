using Nox.Cli.Abstractions;

namespace Nox.Cli.Variables;

public class Variable: IVariable
{
    public Variable(object? value, bool isSecret = false)
    {
        Value = value;
        IsSecret = isSecret;
    }
    
    public object? Value { get; set; } = null;
    public bool IsSecret { get; set; } = false;

    public string DisplayValue
    {
        get
        {
            if (Value == null) return "";
            return IsSecret ? new string('*', Math.Min(20, Value.ToString()!.Length)) : Value.ToString()!;
        }
    }
}