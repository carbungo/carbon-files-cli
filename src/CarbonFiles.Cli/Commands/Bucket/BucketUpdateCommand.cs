using System.ComponentModel;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using CarbonFiles.Client.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Bucket;

public sealed class BucketUpdateCommand(CarbonFilesClient client, IAnsiConsole console)
    : AsyncCommand<BucketUpdateCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("Bucket ID to update.")]
        public string Id { get; init; } = null!;

        [CommandOption("-n|--name <NAME>")]
        [Description("New name for the bucket.")]
        public string? Name { get; init; }

        [CommandOption("-d|--description <DESC>")]
        [Description("New description for the bucket.")]
        public string? Description { get; init; }

        [CommandOption("-e|--expires <EXPIRY>")]
        [Description("New expiry duration (e.g. 1h, 7d, 30d).")]
        public string? Expires { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        if (settings.Name is null && settings.Description is null && settings.Expires is null)
        {
            console.MarkupLine("[red]At least one of --name, --description, or --expires must be provided.[/]");
            return 1;
        }

        var request = new UpdateBucketRequest
        {
            Name = settings.Name,
            Description = settings.Description,
            ExpiresIn = settings.Expires,
        };

        if (settings.Json)
        {
            var b = await client.Buckets[settings.Id].UpdateAsync(request, cancellation);
            console.WriteLine(JsonOutput.Serialize(b));
            return 0;
        }

        var bucket = await console.Status().StartAsync($"{Theme.Sparkles} Updating bucket...", async _ =>
            await client.Buckets[settings.Id].UpdateAsync(request, cancellation));

        var panel = new Panel(
            $"ID:          [blue]{bucket.Id}[/]\n" +
            $"Name:        {Markup.Escape(bucket.Name)}\n" +
            $"Owner:       [cyan]{Markup.Escape(bucket.Owner)}[/]\n" +
            $"Description: {Markup.Escape(bucket.Description ?? "-")}")
        {
            Header = new PanelHeader($"[green]{Theme.Sparkles} Bucket updated[/]"),
        };

        console.Write(panel);

        return 0;
    }
}
