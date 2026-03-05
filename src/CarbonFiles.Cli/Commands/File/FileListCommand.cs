using System.ComponentModel;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using CarbonFiles.Client.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Files;

public sealed class FileListCommand(CarbonFilesClient client, IAnsiConsole console)
    : AsyncCommand<FileListCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<bucket-id>")]
        [Description("Bucket ID to list files from.")]
        public string BucketId { get; init; } = null!;

        [CommandOption("--path <DIR>")]
        [Description("Directory path to list (enables directory listing mode).")]
        public string? Path { get; init; }

        [CommandOption("--limit <LIMIT>")]
        [Description("Maximum number of files to return.")]
        [DefaultValue(50)]
        public int Limit { get; init; }

        [CommandOption("--offset <OFFSET>")]
        [Description("Number of files to skip.")]
        [DefaultValue(0)]
        public int Offset { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        if (settings.Path is not null)
        {
            return await ListDirectoryAsync(settings, cancellation);
        }

        return await ListAllFilesAsync(settings, cancellation);
    }

    private async Task<int> ListAllFilesAsync(Settings settings, CancellationToken cancellation)
    {
        var pagination = new PaginationOptions
        {
            Limit = settings.Limit,
            Offset = settings.Offset,
        };

        var result = await client.Buckets[settings.BucketId].Files.ListAsync(pagination, cancellation);

        if (settings.Json)
        {
            console.WriteLine(JsonOutput.Serialize(result));
            return 0;
        }

        if (result.Items.Count == 0)
        {
            console.MarkupLine("[yellow]No files found.[/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn(new TableColumn("[bold]Path[/]"));
        table.AddColumn(new TableColumn("[bold]Size[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Type[/]"));
        table.AddColumn(new TableColumn("[bold]Created[/]"));

        foreach (var file in result.Items)
        {
            table.AddRow(
                Markup.Escape(file.Path),
                Formatting.FormatSize(file.Size),
                Markup.Escape(file.MimeType),
                Formatting.FormatDate(file.CreatedAt.UtcDateTime));
        }

        console.Write(table);
        console.MarkupLine($"[dim]Showing {result.Items.Count} of {result.Total}[/]");

        return 0;
    }

    private async Task<int> ListDirectoryAsync(Settings settings, CancellationToken cancellation)
    {
        var pagination = new PaginationOptions
        {
            Limit = settings.Limit,
            Offset = settings.Offset,
        };

        var result = await client.Buckets[settings.BucketId].Files.ListDirectoryAsync(
            settings.Path, pagination, cancellation);

        if (settings.Json)
        {
            console.WriteLine(JsonOutput.Serialize(result));
            return 0;
        }

        var totalItems = result.Folders.Count + result.Files.Count;

        if (totalItems == 0)
        {
            console.MarkupLine("[yellow]No files found.[/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn(new TableColumn("[bold]Path[/]"));
        table.AddColumn(new TableColumn("[bold]Size[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Type[/]"));
        table.AddColumn(new TableColumn("[bold]Created[/]"));

        foreach (var folder in result.Folders)
        {
            table.AddRow(
                $"[blue]{Markup.Escape(folder)}[/]",
                "-",
                "folder",
                "-");
        }

        foreach (var file in result.Files)
        {
            table.AddRow(
                Markup.Escape(file.Path),
                Formatting.FormatSize(file.Size),
                Markup.Escape(file.MimeType),
                Formatting.FormatDate(file.CreatedAt.UtcDateTime));
        }

        console.Write(table);
        var totalCount = result.TotalFiles + result.TotalFolders;
        console.MarkupLine($"[dim]Showing {totalItems} of {totalCount}[/]");

        return 0;
    }
}
