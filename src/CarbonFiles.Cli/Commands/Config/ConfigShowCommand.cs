using CarbonFiles.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Config;

public sealed class ConfigShowCommand(CliConfiguration config, IAnsiConsole console)
    : AsyncCommand<GlobalSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellation)
    {
        var profileName = settings.Profile ?? config.ActiveProfile;
        var profile = config.Profiles.GetValueOrDefault(profileName);

        if (profile is null)
        {
            console.MarkupLine("[yellow]No profile configured.[/]");
            console.MarkupLine("Run [blue]cf config set --url <URL> --token <TOKEN>[/] to get started.");
            return Task.FromResult(0);
        }

        var table = new Table();
        table.AddColumn("Setting");
        table.AddColumn("Value");
        table.AddRow("Profile", profileName);
        table.AddRow("URL", profile.Url);
        table.AddRow("Token", profile.MaskedToken);
        table.AddRow("Frontend", profile.FrontendUrl ?? "[dim]-[/]");

        console.Write(table);
        return Task.FromResult(0);
    }
}
