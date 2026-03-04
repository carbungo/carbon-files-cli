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

        [CommandOption("--frontend-url <URL>")]
        [Description("Frontend dashboard URL (e.g. https://dash.carbun.xyz).")]
        public string? FrontendUrl { get; init; }
    }

    public override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var profileName = settings.Profile ?? config.ActiveProfile;

        // If only --frontend-url is being set, update just that field
        if (settings.FrontendUrl is not null && settings.Url is null && settings.Token is null)
        {
            if (!config.Profiles.ContainsKey(profileName))
            {
                console.MarkupLine($"[red]Profile '{profileName}' does not exist. Set --url and --token first.[/]");
                return Task.FromResult(1);
            }

            config.UpdateFrontendUrl(profileName, settings.FrontendUrl);
            config.Save();

            console.MarkupLine($"[green]Frontend URL updated for profile '{profileName}'.[/]");
            console.MarkupLine($"  Frontend: {settings.FrontendUrl.TrimEnd('/')}");
            return Task.FromResult(0);
        }

        if (string.IsNullOrWhiteSpace(settings.Url) || string.IsNullOrWhiteSpace(settings.Token))
        {
            console.MarkupLine("[red]Both --url and --token are required.[/]");
            return Task.FromResult(1);
        }

        config.SetProfile(profileName, settings.Url, settings.Token, settings.FrontendUrl);
        config.Save();

        var profile = config.Profiles[profileName];
        console.MarkupLine($"[green]Profile '{profileName}' saved.[/]");
        console.MarkupLine($"  URL:      {profile.Url}");
        console.MarkupLine($"  Token:    {profile.MaskedToken}");
        if (profile.FrontendUrl is not null)
            console.MarkupLine($"  Frontend: {profile.FrontendUrl}");

        return Task.FromResult(0);
    }
}
