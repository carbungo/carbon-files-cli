using System.ComponentModel;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Bucket;

public sealed class BucketWatchCommand(ApiClientFactory factory, IAnsiConsole console)
    : AsyncCommand<BucketWatchCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("Bucket ID to watch for live changes.")]
        public string Id { get; init; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var client = factory.Create(settings.Profile);
        var events = client.Events;

        events.OnFileCreated((bucketId, file) =>
        {
            console.MarkupLine($"[green]+[/] [green]FileCreated[/]: {Markup.Escape(file.Path)} ({Formatting.FormatSize(file.Size)})");
            return Task.CompletedTask;
        });
        events.OnFileUpdated((bucketId, file) =>
        {
            console.MarkupLine($"[yellow]~[/] [yellow]FileUpdated[/]: {Markup.Escape(file.Path)} ({Formatting.FormatSize(file.Size)})");
            return Task.CompletedTask;
        });
        events.OnFileDeleted((bucketId, path) =>
        {
            console.MarkupLine($"[red]-[/] [red]FileDeleted[/]: {Markup.Escape(path)}");
            return Task.CompletedTask;
        });
        events.OnBucketUpdated((bucketId, changes) =>
        {
            console.MarkupLine($"[blue]B[/] [blue]BucketUpdated[/]: {Markup.Escape(bucketId)}");
            return Task.CompletedTask;
        });
        events.OnBucketDeleted(bucketId =>
        {
            console.MarkupLine($"[red]X[/] [red]BucketDeleted[/]: {Markup.Escape(bucketId)}");
            return Task.CompletedTask;
        });

        await events.ConnectAsync(cancellation);
        await events.SubscribeToBucketAsync(settings.Id, cancellation);

        console.MarkupLine($"[bold]Watching bucket {Markup.Escape(settings.Id)} for changes...[/]");
        console.MarkupLine("[dim]Press Ctrl+C to stop.[/]");

        try
        {
            await Task.Delay(Timeout.Infinite, cancellation);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        finally
        {
            await events.DisposeAsync();
        }

        return 0;
    }
}
