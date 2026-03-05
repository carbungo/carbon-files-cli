using System.ComponentModel;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using CarbonFiles.Client.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Token;

public sealed class TokenCreateDashboardCommand(CarbonFilesClient client, IAnsiConsole console)
    : AsyncCommand<TokenCreateDashboardCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandOption("-e|--expires <EXPIRY>")]
        [Description("Token expiry (e.g. 1h, 12h, 24h).")]
        public string? Expires { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var request = new CreateDashboardTokenRequest
        {
            ExpiresIn = settings.Expires,
        };

        if (settings.Json)
        {
            var r = await client.Dashboard.CreateTokenAsync(request, cancellation);
            console.WriteLine(JsonOutput.Serialize(r));
            return 0;
        }

        var result = await console.Status().StartAsync($"{Theme.Rocket} Creating dashboard token...", async _ =>
            await client.Dashboard.CreateTokenAsync(request, cancellation));

        var panel = new Panel(
            new Rows(
                new Markup(""),
                new Markup($"  Token:   [bold green]{Markup.Escape(result.Token)}[/]"),
                new Markup($"  Expires: {Formatting.FormatExpiry(result.ExpiresAt.UtcDateTime)}"),
                new Markup("")))
        {
            Header = new PanelHeader($"{Theme.Rocket} Dashboard Token Created"),
            Border = BoxBorder.Rounded,
        };

        console.Write(panel);

        return 0;
    }
}
