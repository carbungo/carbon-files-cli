using System.ComponentModel;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Short;

public sealed class ShortDeleteCommand(CarbonFilesClient client, IAnsiConsole console)
    : AsyncCommand<ShortDeleteCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<code>")]
        [Description("Short URL code to delete.")]
        public string Code { get; init; } = null!;

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

            if (!console.Confirm($"Are you sure you want to delete short URL '[yellow]{Markup.Escape(settings.Code)}[/]'?", defaultValue: false))
            {
                console.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        await console.Status().StartAsync($"{Theme.Skull} Deleting...", async _ =>
            await client.ShortUrls[settings.Code].DeleteAsync(cancellation));

        console.MarkupLine($"{Theme.Skull} Poof. Short URL [bold]'{Markup.Escape(settings.Code)}'[/] is gone.");

        return 0;
    }
}
