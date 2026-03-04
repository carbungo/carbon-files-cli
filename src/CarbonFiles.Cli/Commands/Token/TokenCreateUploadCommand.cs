using System.ComponentModel;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Token;

public sealed class TokenCreateUploadCommand(ICarbonFilesApi api, IAnsiConsole console)
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

        var result = await api.Tokens(settings.BucketId, request, cancellation);

        var panel = new Panel(
            new Rows(
                new Markup(""),
                new Markup($"  Token:       [bold green]{Markup.Escape(result.Token)}[/]"),
                new Markup($"  Bucket:      {Markup.Escape(result.BucketId)}"),
                new Markup($"  Expires:     {Formatting.FormatExpiry(result.ExpiresAt.UtcDateTime)}"),
                new Markup($"  Max Uploads: {(result.MaxUploads.HasValue ? result.MaxUploads.Value.ToString() : "Unlimited")}"),
                new Markup("")))
        {
            Header = new PanelHeader("Upload Token Created"),
            Border = BoxBorder.Rounded,
        };

        console.Write(panel);

        return 0;
    }
}
