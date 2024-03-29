﻿using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Configuration;

public class CliConfiguration
{
    public string Branch { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string? CommandAlias { get; set; }
    public string? Description { get; set; }
    public List<string[]>? Examples { get; set; }
}

