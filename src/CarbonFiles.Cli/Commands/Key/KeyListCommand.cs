using System.ComponentModel;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using CarbonFiles.Client.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Key;

public sealed class KeyListCommand(CarbonFilesClient client, IAnsiConsole console)
    : AsyncCommand<KeyListCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandOption("--limit <LIMIT>")]
        [Description("Maximum number of keys to return.")]
        [DefaultValue(50)]
        public int Limit { get; init; }

        [CommandOption("--offset <OFFSET>")]
        [Description("Number of keys to skip.")]
        [DefaultValue(0)]
        public int Offset { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var pagination = new PaginationOptions
        {
            Limit = settings.Limit,
            Offset = settings.Offset,
        };

        var result = await client.Keys.ListAsync(pagination, cancellation);

        if (settings.Json)
        {
            console.WriteLine(JsonOutput.Serialize(result));
            return 0;
        }

        if (result.Items.Count == 0)
        {
            console.MarkupLine("[yellow]No API keys found.[/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn(new TableColumn("[bold blue]Prefix[/]"));
        table.AddColumn(new TableColumn("[bold]Name[/]"));
        table.AddColumn(new TableColumn("[bold]Created[/]"));
        table.AddColumn(new TableColumn("[bold]Last Used[/]"));
        table.AddColumn(new TableColumn("[bold]Buckets[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Files[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Size[/]").RightAligned());

        foreach (var key in result.Items)
        {
            table.AddRow(
                $"[blue]{Markup.Escape(key.Prefix)}[/]",
                Markup.Escape(key.Name),
                Formatting.FormatDate(key.CreatedAt.UtcDateTime),
                Formatting.FormatDate(key.LastUsedAt?.UtcDateTime),
                key.BucketCount.ToString(),
                key.FileCount.ToString(),
                Formatting.FormatSize(key.TotalSize));
        }

        console.Write(table);
        console.MarkupLine($"[dim]Showing {result.Items.Count} of {result.Total}[/]");

        return 0;
    }
}
