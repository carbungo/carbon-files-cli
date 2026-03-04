using System.ComponentModel;
using CarbonFiles.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Config;

public sealed class ConfigSetCommand(CliConfiguration config, IAnsiConsole console)
    : AsyncCommand<ConfigSetCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandOption("--url <URL>")]
        [Description("Server URL to connect to.")]
        public string? Url { get; init; }

        [CommandOption("--token <TOKEN>")]
        [Description("Authentication token (API key or admin key).")]
        public string? Token { get; init; }
    }

    public override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(settings.Url) || string.IsNullOrWhiteSpace(settings.Token))
        {
            console.MarkupLine("[red]Both --url and --token are required.[/]");
            return Task.FromResult(1);
        }

        var profileName = settings.Profile ?? config.ActiveProfile;
        config.SetProfile(profileName, settings.Url, settings.Token);
        config.Save();

        var profile = config.Profiles[profileName];
        console.MarkupLine($"[green]Profile '{profileName}' saved.[/]");
        console.MarkupLine($"  URL:   {profile.Url}");
        console.MarkupLine($"  Token: {profile.MaskedToken}");

        return Task.FromResult(0);
    }
}
