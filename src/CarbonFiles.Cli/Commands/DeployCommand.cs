using System.ComponentModel;
using CarbonFiles.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands;

public sealed class DeployCommand(ApiClientFactory factory, IAnsiConsole console) : AsyncCommand<DeployCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<directory>")]
        [Description("Local static site directory.")]
        public string Directory { get; init; } = null!;

        [CommandArgument(1, "<bucket-id>")]
        [Description("Destination bucket ID.")]
        public string BucketId { get; init; } = null!;

        [CommandOption("--token <TOKEN>")]
        [Description("Upload token or API key override.")]
        public string? Token { get; init; }

        [CommandOption("--dry-run")]
        [Description("Show uploads and deletions without changing the bucket.")]
        public bool DryRun { get; init; }

        [CommandOption("--no-prune")]
        [Description("Keep remote files absent from the local directory.")]
        public bool NoPrune { get; init; }

        [CommandOption("--concurrency <COUNT>")]
        [Description("Maximum parallel uploads (default: 4).")]
        [DefaultValue(4)]
        public int Concurrency { get; init; } = 4;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        if (settings.Concurrency is < 1 or > 32)
        {
            console.MarkupLine("[red]Concurrency must be between 1 and 32.[/]");
            return 1;
        }
        try
        {
            var profile = factory.GetProfile(settings.Profile, settings.Token);
            using var http = factory.CreateHttpClient(settings.Profile, settings.Token);
            var outputLock = new object();
            var service = new DeployService(http, settings.BucketId, settings.Token ?? profile.Token,
                line => { lock (outputLock) console.WriteLine(line); });
            var result = await service.DeployAsync(settings.Directory, settings.DryRun, !settings.NoPrune, settings.Concurrency, cancellation);
            console.WriteLine($"{(settings.DryRun ? "Dry run" : "Deployed")}: {result.Uploaded} uploaded, {result.Skipped} skipped, {result.Pruned} pruned; {result.Bytes} bytes; {result.Elapsed.TotalSeconds:F1}s.");
            return 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Avoid exception messages from HTTP libraries, which can include a token-bearing URL.
            var message = ex is InvalidOperationException or IOException or UnauthorizedAccessException ? ex.Message : ex.GetType().Name;
            console.MarkupLine($"[red]Deploy failed: {Markup.Escape(SecretRedaction.Redact(message))}[/]");
            return 1;
        }
    }
}
