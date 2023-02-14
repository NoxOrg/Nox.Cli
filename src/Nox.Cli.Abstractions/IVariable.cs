namespace Nox.Cli.Abstractions;

public interface IVariable
{
    object? Value { get; set; }
    bool IsSecret { get; set; }
    string DisplayValue { get; }
}