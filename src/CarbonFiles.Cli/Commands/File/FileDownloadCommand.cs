using System.ComponentModel;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Files;

public sealed class FileDownloadCommand(CarbonFilesClient client, IAnsiConsole console)
    : AsyncCommand<FileDownloadCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<bucket-id>")]
        [Description("Bucket ID containing the file.")]
        public string BucketId { get; init; } = null!;

        [CommandArgument(1, "<path>")]
        [Description("Remote file path to download.")]
        public string Path { get; init; } = null!;

        [CommandOption("-o|--output <PATH>")]
        [Description("Local output path (defaults to filename portion of remote path).")]
        public string? Output { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var remotePath = settings.Path;
        var outputPath = settings.Output ?? System.IO.Path.GetFileName(remotePath);

        if (string.IsNullOrEmpty(outputPath))
        {
            outputPath = "download";
        }

        await using var stream = await client.Buckets[settings.BucketId].Files[remotePath].DownloadAsync(cancellation);
        await using var fileStream = File.Create(outputPath);

        await console.Progress().StartAsync(async ctx =>
        {
            var task = ctx.AddTask($"Downloading [blue]{Markup.Escape(System.IO.Path.GetFileName(outputPath))}[/]");
            task.IsIndeterminate = true;

            var buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, cancellation)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellation);
                task.Increment(bytesRead);
            }
        });

        var fileInfo = new FileInfo(outputPath);
        console.MarkupLine($"[green]Downloaded to {Markup.Escape(outputPath)} ({Formatting.FormatSize(fileInfo.Length)})[/]");
        return 0;
    }
}
