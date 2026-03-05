using System.ComponentModel;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using CarbonFiles.Client.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Bucket;

public sealed class BucketCreateCommand(CarbonFilesClient client, ApiClientFactory factory, IAnsiConsole console)
    : AsyncCommand<BucketCreateCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<name>")]
        [Description("Name for the new bucket.")]
        public string Name { get; init; } = null!;

        [CommandOption("-d|--description <DESC>")]
        [Description("Optional description for the bucket.")]
        public string? Description { get; init; }

        [CommandOption("-e|--expires <EXPIRY>")]
        [Description("Expiry duration (e.g. 1h, 7d, 30d).")]
        public string? Expires { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var request = new CreateBucketRequest
        {
            Name = settings.Name,
            Description = settings.Description,
            ExpiresIn = settings.Expires,
        };

        var bucket = await client.Buckets.CreateAsync(request, cancellation);

        if (settings.Json)
        {
            console.WriteLine(JsonOutput.Serialize(bucket));
            return 0;
        }

        var content = $"ID:    [blue]{bucket.Id}[/]\n" +
            $"Name:  {Markup.Escape(bucket.Name)}\n" +
            $"Owner: [cyan]{Markup.Escape(bucket.Owner)}[/]";

        var links = new LinkBuilder(factory.GetProfile(settings.Profile));
        if (links.HasFrontend)
            content += $"\nLink:  [link={links.BucketUrl(bucket.Id)}]{Markup.Escape(links.BucketUrl(bucket.Id))}[/]";

        var panel = new Panel(content)
        {
            Header = new PanelHeader("[green]Bucket Created[/]"),
        };

        console.Write(panel);

        return 0;
    }
}
