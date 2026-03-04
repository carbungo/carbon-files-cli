using System.ComponentModel;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Bucket;

public sealed class BucketListCommand(ICarbonFilesApi api, IAnsiConsole console)
    : AsyncCommand<BucketListCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandOption("--include-expired")]
        [Description("Include expired buckets in the listing.")]
        [DefaultValue(false)]
        public bool IncludeExpired { get; init; }

        [CommandOption("--limit <LIMIT>")]
        [Description("Maximum number of buckets to return.")]
        [DefaultValue(50)]
        public int Limit { get; init; }

        [CommandOption("--offset <OFFSET>")]
        [Description("Number of buckets to skip.")]
        [DefaultValue(0)]
        public int Offset { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var result = await api.BucketsGET(
            settings.Limit,
            settings.Offset,
            null,
            null,
            settings.IncludeExpired ? true : null,
            cancellation);

        if (result.Items.Count == 0)
        {
            console.MarkupLine("[yellow]No buckets found.[/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn(new TableColumn("[bold blue]ID[/]"));
        table.AddColumn(new TableColumn("[bold]Name[/]"));
        table.AddColumn(new TableColumn("[bold cyan]Owner[/]"));
        table.AddColumn(new TableColumn("[bold]Files[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Size[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Expires[/]"));

        foreach (var bucket in result.Items)
        {
            table.AddRow(
                $"[blue]{bucket.Id}[/]",
                Markup.Escape(bucket.Name),
                $"[cyan]{Markup.Escape(bucket.Owner)}[/]",
                bucket.FileCount.ToString(),
                Formatting.FormatSize(bucket.TotalSize),
                Formatting.FormatExpiry(bucket.ExpiresAt?.UtcDateTime));
        }

        console.Write(table);
        console.MarkupLine($"[dim]Showing {result.Items.Count} of {result.Total}[/]");

        return 0;
    }
}
