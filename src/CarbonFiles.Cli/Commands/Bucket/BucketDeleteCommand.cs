using System.ComponentModel;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Bucket;

public sealed class BucketDeleteCommand(CarbonFilesClient client, IAnsiConsole console)
    : AsyncCommand<BucketDeleteCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("Bucket ID to delete.")]
        public string Id { get; init; } = null!;

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

            if (!console.Confirm($"Are you sure you want to delete bucket '[yellow]{Markup.Escape(settings.Id)}[/]'?", defaultValue: false))
            {
                console.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        await console.Status().StartAsync($"{Theme.Skull} Deleting...", async _ =>
            await client.Buckets[settings.Id].DeleteAsync(cancellation));

        console.MarkupLine($"{Theme.Skull} Poof. [bold]'{Markup.Escape(settings.Id)}'[/] is gone.");

        return 0;
    }
}
