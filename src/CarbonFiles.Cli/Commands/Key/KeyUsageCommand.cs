using System.ComponentModel;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Key;

public sealed class KeyUsageCommand(ICarbonFilesApi api, IAnsiConsole console)
    : AsyncCommand<KeyUsageCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<prefix>")]
        [Description("API key prefix to show usage for.")]
        public string Prefix { get; init; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var result = await api.Usage(settings.Prefix, cancellation);

        if (settings.Json)
        {
            console.WriteLine(JsonOutput.Serialize(result));
            return 0;
        }

        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow("[bold]Prefix[/]", Markup.Escape(result.Prefix));
        grid.AddRow("[bold]Name[/]", Markup.Escape(result.Name));
        grid.AddRow("[bold]Created[/]", Formatting.FormatDate(result.CreatedAt.UtcDateTime));
        grid.AddRow("[bold]Last Used[/]", Formatting.FormatDate(result.LastUsedAt?.UtcDateTime));
        grid.AddRow("[bold]Buckets[/]", result.BucketCount.ToString());
        grid.AddRow("[bold]Files[/]", result.FileCount.ToString());
        grid.AddRow("[bold]Total Size[/]", Formatting.FormatSize(result.TotalSize));
        grid.AddRow("[bold]Total Downloads[/]", result.TotalDownloads.ToString());

        var panel = new Panel(grid)
        {
            Header = new PanelHeader("API Key Usage"),
            Border = BoxBorder.Rounded,
        };

        console.Write(panel);

        if (result.Buckets.Count > 0)
        {
            console.WriteLine();

            var table = new Table();
            table.AddColumn(new TableColumn("[bold blue]Bucket ID[/]"));
            table.AddColumn(new TableColumn("[bold]Name[/]"));
            table.AddColumn(new TableColumn("[bold]Files[/]").RightAligned());
            table.AddColumn(new TableColumn("[bold]Size[/]").RightAligned());

            foreach (var bucket in result.Buckets)
            {
                table.AddRow(
                    $"[blue]{Markup.Escape(bucket.Id)}[/]",
                    Markup.Escape(bucket.Name),
                    bucket.FileCount.ToString(),
                    Formatting.FormatSize(bucket.TotalSize));
            }

            console.Write(table);
        }

        return 0;
    }
}
