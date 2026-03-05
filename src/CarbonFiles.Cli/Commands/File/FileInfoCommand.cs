using System.ComponentModel;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Files;

public sealed class FileInfoCommand(CarbonFilesClient client, ApiClientFactory factory, IAnsiConsole console)
    : AsyncCommand<FileInfoCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<bucket-id>")]
        [Description("Bucket ID containing the file.")]
        public string BucketId { get; init; } = null!;

        [CommandArgument(1, "<path>")]
        [Description("File path within the bucket.")]
        public string Path { get; init; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var file = await client.Buckets[settings.BucketId].Files[settings.Path].GetMetadataAsync(cancellation);

        if (settings.Json)
        {
            console.WriteLine(JsonOutput.Serialize(file));
            return 0;
        }

        var table = new Table().NoBorder().HideHeaders();
        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("Path", Markup.Escape(file.Path));
        table.AddRow("Name", Markup.Escape(file.Name));
        table.AddRow("Size", Formatting.FormatSize(file.Size));
        table.AddRow("MIME Type", Markup.Escape(file.MimeType));
        table.AddRow("SHA-256", Markup.Escape(file.Sha256 ?? "-"));
        table.AddRow("Short Code", Markup.Escape(file.ShortCode ?? "-"));
        table.AddRow("Short URL", Markup.Escape(file.ShortUrl ?? "-"));

        var links = new LinkBuilder(factory.GetProfile(settings.Profile));
        if (links.HasFrontend)
            table.AddRow("Link", $"[link={links.FileUrl(settings.BucketId, file.Path)}]{Markup.Escape(links.FileUrl(settings.BucketId, file.Path))}[/]");

        table.AddRow("Created", Formatting.FormatDate(file.CreatedAt.UtcDateTime));
        table.AddRow("Updated", Formatting.FormatDate(file.UpdatedAt.UtcDateTime));

        console.Write(table);

        return 0;
    }
}
