namespace Nox.Cli.Abstractions;

public interface IActionConfiguration
{
    string Id { get; set; }
    string? If { get; set; }
    string Name { get; set; }
    string Uses { get; set; }
    Dictionary<string, object> With { get; set; }
    Dictionary<string, string>? Validate { get; set; }
    NoxActionDisplayMessage? Display { get; set; }
    bool ContinueOnError { get; set; }
}