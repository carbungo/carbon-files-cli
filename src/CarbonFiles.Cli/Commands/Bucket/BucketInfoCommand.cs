using System.ComponentModel;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Bucket;

public sealed class BucketInfoCommand(ICarbonFilesApi api, IAnsiConsole console)
    : AsyncCommand<BucketInfoCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("Bucket ID to look up.")]
        public string Id { get; init; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var bucket = await api.BucketsGET2(settings.Id, cancellation);

        if (settings.Json)
        {
            console.WriteLine(JsonOutput.Serialize(bucket));
            return 0;
        }

        var infoTable = new Table().NoBorder().HideHeaders();
        infoTable.AddColumn("Property");
        infoTable.AddColumn("Value");

        infoTable.AddRow("ID", $"[blue]{bucket.Id}[/]");
        infoTable.AddRow("Name", Markup.Escape(bucket.Name));
        infoTable.AddRow("Owner", $"[cyan]{Markup.Escape(bucket.Owner)}[/]");
        infoTable.AddRow("Description", Markup.Escape(bucket.Description ?? "-"));
        infoTable.AddRow("Files", bucket.FileCount.ToString());
        infoTable.AddRow("Total Size", Formatting.FormatSize(bucket.TotalSize));
        infoTable.AddRow("Created", Formatting.FormatDate(bucket.CreatedAt.UtcDateTime));
        infoTable.AddRow("Expires", Formatting.FormatExpiry(bucket.ExpiresAt?.UtcDateTime));

        console.Write(infoTable);

        if (bucket.Files.Count > 0)
        {
            console.WriteLine();

            var fileTable = new Table();
            fileTable.AddColumn("Path");
            fileTable.AddColumn(new TableColumn("Size").RightAligned());
            fileTable.AddColumn("Type");
            fileTable.AddColumn("Short URL");

            foreach (var file in bucket.Files)
            {
                fileTable.AddRow(
                    Markup.Escape(file.Path),
                    Formatting.FormatSize(file.Size),
                    Markup.Escape(file.MimeType),
                    Markup.Escape(file.ShortUrl ?? "-"));
            }

            console.Write(fileTable);
        }

        if (bucket.HasMoreFiles)
        {
            console.MarkupLine($"[dim]More files available. Use: cf file list {bucket.Id}[/]");
        }

        return 0;
    }
}
