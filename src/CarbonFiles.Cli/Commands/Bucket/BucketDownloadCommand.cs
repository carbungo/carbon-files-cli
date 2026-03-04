using System.ComponentModel;
using CarbonFiles.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Bucket;

public sealed class BucketDownloadCommand(ApiClientFactory factory, IAnsiConsole console)
    : AsyncCommand<BucketDownloadCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("Bucket ID to download.")]
        public string Id { get; init; } = null!;

        [CommandOption("-o|--output <PATH>")]
        [Description("Output file path (defaults to {id}.zip).")]
        public string? Output { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var outputPath = settings.Output ?? $"{settings.Id}.zip";
        using var client = factory.CreateHttpClient(settings.Profile);

        using var response = await client.GetAsync(
            $"/api/buckets/{settings.Id}/zip",
            HttpCompletionOption.ResponseHeadersRead,
            cancellation);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var stream = await response.Content.ReadAsStreamAsync(cancellation);
        await using var fileStream = File.Create(outputPath);

        await console.Progress().StartAsync(async ctx =>
        {
            var task = ctx.AddTask($"Downloading [blue]{Markup.Escape(settings.Id)}.zip[/]",
                maxValue: totalBytes ?? 0);
            task.IsIndeterminate = totalBytes is null;

            var buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, cancellation)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellation);
                task.Increment(bytesRead);
            }
        });

        console.MarkupLine($"[green]Downloaded to {Markup.Escape(outputPath)}[/]");
        return 0;
    }
}
