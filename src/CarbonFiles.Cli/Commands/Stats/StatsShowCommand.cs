using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Stats;

public sealed class StatsShowCommand(CarbonFilesClient client, IAnsiConsole console)
    : AsyncCommand<GlobalSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellation)
    {
        var stats = await client.Stats.GetAsync(cancellation);

        if (settings.Json)
        {
            console.WriteLine(JsonOutput.Serialize(stats));
            return 0;
        }

        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow("[bold]Buckets[/]", $"[cyan]{stats.TotalBuckets}[/]");
        grid.AddRow("[bold]Files[/]", $"[cyan]{stats.TotalFiles}[/]");
        grid.AddRow("[bold]Total Size[/]", $"[cyan]{Formatting.FormatSize(stats.TotalSize)}[/]");
        grid.AddRow("[bold]API Keys[/]", $"[cyan]{stats.TotalKeys}[/]");
        grid.AddRow("[bold]Downloads[/]", $"[cyan]{stats.TotalDownloads}[/]");

        var panel = new Panel(grid)
            .Header("[bold]System Statistics[/]")
            .Border(BoxBorder.Rounded);

        console.Write(panel);

        if (stats.StorageByOwner.Count > 0)
        {
            console.WriteLine();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold]Storage by Owner[/]");

            table.AddColumn("[bold]Owner[/]");
            table.AddColumn(new TableColumn("[bold]Buckets[/]").RightAligned());
            table.AddColumn(new TableColumn("[bold]Files[/]").RightAligned());
            table.AddColumn(new TableColumn("[bold]Size[/]").RightAligned());

            foreach (var owner in stats.StorageByOwner)
            {
                table.AddRow(
                    Markup.Escape(owner.Owner),
                    owner.BucketCount.ToString(),
                    owner.FileCount.ToString(),
                    Formatting.FormatSize(owner.TotalSize));
            }

            console.Write(table);
        }

        return 0;
    }
}
