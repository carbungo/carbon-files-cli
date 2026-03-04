using System.ComponentModel;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Files;

public sealed class FileDeleteCommand(ICarbonFilesApi api, IAnsiConsole console)
    : AsyncCommand<FileDeleteCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<bucket-id>")]
        [Description("Bucket ID containing the file.")]
        public string BucketId { get; init; } = null!;

        [CommandArgument(1, "<path>")]
        [Description("File path to delete.")]
        public string Path { get; init; } = null!;

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

            if (!console.Confirm($"Are you sure you want to delete file '[yellow]{Markup.Escape(settings.Path)}[/]' from bucket '[yellow]{Markup.Escape(settings.BucketId)}[/]'?", defaultValue: false))
            {
                console.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        await api.FilesDELETE(settings.BucketId, settings.Path, cancellation);

        console.MarkupLine($"[green]Deleted file '{Markup.Escape(settings.Path)}' from bucket '{Markup.Escape(settings.BucketId)}'.[/]");

        return 0;
    }
}
