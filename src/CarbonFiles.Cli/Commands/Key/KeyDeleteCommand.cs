using System.ComponentModel;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Key;

public sealed class KeyDeleteCommand(CarbonFilesClient client, IAnsiConsole console)
    : AsyncCommand<KeyDeleteCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<prefix>")]
        [Description("API key prefix to delete.")]
        public string Prefix { get; init; } = null!;

        [CommandOption("-y|--yes")]
        [Description("Skip confirmation prompt.")]
        [DefaultValue(false)]
        public bool Yes { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        if (!settings.Yes)
        {
            if (!console.Profile.Capabilities.Interactive)
            {
                console.MarkupLine("[red]Cannot prompt for confirmation in non-interactive mode. Use --yes to confirm.[/]");
                return 1;
            }

            if (!console.Confirm($"Are you sure you want to delete API key '[yellow]{Markup.Escape(settings.Prefix)}[/]'?", defaultValue: false))
            {
                console.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        await console.Status().StartAsync($"{Theme.Skull} Deleting...", async _ =>
            await client.Keys[settings.Prefix].RevokeAsync(cancellation));

        console.MarkupLine($"{Theme.Skull} Poof. API key [bold]'{Markup.Escape(settings.Prefix)}'[/] is gone.");

        return 0;
    }
}
