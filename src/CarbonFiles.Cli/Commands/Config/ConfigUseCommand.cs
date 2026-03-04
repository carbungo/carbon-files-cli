using System.ComponentModel;
using CarbonFiles.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Config;

public sealed class ConfigUseCommand(CliConfiguration config, IAnsiConsole console)
    : AsyncCommand<ConfigUseCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<name>")]
        [Description("Profile name to switch to.")]
        public string Name { get; init; } = string.Empty;
    }

    public override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        if (!config.Profiles.ContainsKey(settings.Name))
        {
            console.MarkupLine($"[red]Profile '{settings.Name}' not found.[/]");
            return Task.FromResult(1);
        }

        config.ActiveProfile = settings.Name;
        config.Save();

        console.MarkupLine($"[green]Switched to profile '{settings.Name}'.[/]");
        return Task.FromResult(0);
    }
}
