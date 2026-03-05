using System.ComponentModel;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using CarbonFiles.Client.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace CarbonFiles.Cli.Commands.Token;

public sealed class TokenCreateUploadCommand(CarbonFilesClient client, ApiClientFactory factory, IAnsiConsole console)
    : AsyncCommand<TokenCreateUploadCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<bucket-id>")]
        [Description("The bucket ID to create an upload token for.")]
        public string BucketId { get; init; } = null!;

        [CommandOption("-e|--expires <EXPIRY>")]
        [Description("Token expiry (e.g. 1h, 1d, 1w).")]
        public string? Expires { get; init; }

        [CommandOption("--max-uploads <N>")]
        [Description("Maximum number of uploads allowed.")]
        public int? MaxUploads { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var request = new CreateUploadTokenRequest
        {
            ExpiresIn = settings.Expires,
            MaxUploads = settings.MaxUploads,
        };

        var result = await client.Buckets[settings.BucketId].Tokens.CreateAsync(request, cancellation);

        if (settings.Json)
        {
            console.WriteLine(JsonOutput.Serialize(result));
            return 0;
        }

        var rows = new List<IRenderable>
        {
            new Markup(""),
            new Markup($"  Token:       [bold green]{Markup.Escape(result.Token)}[/]"),
            new Markup($"  Bucket:      {Markup.Escape(result.BucketId)}"),
            new Markup($"  Expires:     {Formatting.FormatExpiry(result.ExpiresAt.UtcDateTime)}"),
            new Markup($"  Max Uploads: {(result.MaxUploads.HasValue ? result.MaxUploads.Value.ToString() : "Unlimited")}"),
        };

        var links = new LinkBuilder(factory.GetProfile(settings.Profile));
        if (links.HasFrontend)
        {
            var uploadUrl = links.UploadUrl(settings.BucketId, result.Token);
            rows.Add(new Markup($"  Upload Link: [link={uploadUrl}]{Markup.Escape(uploadUrl)}[/]"));
        }

        rows.Add(new Markup(""));

        var panel = new Panel(new Rows(rows))
        {
            Header = new PanelHeader("Upload Token Created"),
            Border = BoxBorder.Rounded,
        };

        console.Write(panel);

        return 0;
    }
}
