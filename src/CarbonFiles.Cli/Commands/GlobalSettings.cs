using System.ComponentModel;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands;

public class GlobalSettings : CommandSettings
{
    [CommandOption("--json")]
    [Description("Output raw JSON instead of formatted tables.")]
    [DefaultValue(false)]
    public bool Json { get; init; }

    [CommandOption("--profile <PROFILE>")]
    [Description("Use a specific config profile for this command.")]
    public string? Profile { get; init; }

    [CommandOption("-v|--verbose")]
    [Description("Show HTTP request and response details.")]
    [DefaultValue(false)]
    public bool Verbose { get; init; }
}
