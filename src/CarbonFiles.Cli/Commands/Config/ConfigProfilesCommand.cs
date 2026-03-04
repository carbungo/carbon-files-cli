using CarbonFiles.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Config;

public sealed class ConfigProfilesCommand(CliConfiguration config, IAnsiConsole console)
    : AsyncCommand<GlobalSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellation)
    {
        if (config.Profiles.Count == 0)
        {
            console.MarkupLine("[yellow]No profiles configured.[/]");
            return Task.FromResult(0);
        }

        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("URL");
        table.AddColumn("Token");
        table.AddColumn("Active");

        foreach (var (name, profile) in config.Profiles)
        {
            var active = name == config.ActiveProfile ? "[green]*[/]" : "";
            table.AddRow(name, profile.Url, profile.MaskedToken, active);
        }

        console.Write(table);
        return Task.FromResult(0);
    }
}
