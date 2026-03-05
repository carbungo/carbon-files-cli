using System.ComponentModel;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Bucket;

public sealed class BucketSummaryCommand(CarbonFilesClient client, IAnsiConsole console)
    : AsyncCommand<BucketSummaryCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("Bucket ID to summarize.")]
        public string Id { get; init; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var summary = await console.Status().StartAsync("Fetching summary...", async _ =>
            await client.Buckets[settings.Id].GetSummaryAsync(cancellation));

        console.WriteLine(summary);
        return 0;
    }
}
