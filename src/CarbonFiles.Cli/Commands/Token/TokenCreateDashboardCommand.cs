using System.ComponentModel;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Token;

public sealed class TokenCreateDashboardCommand(ICarbonFilesApi api, IAnsiConsole console)
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

        var result = await api.Dashboard(request, cancellation);

        if (settings.Json)
        {
            console.WriteLine(JsonOutput.Serialize(result));
            return 0;
        }

        var panel = new Panel(
            new Rows(
                new Markup(""),
                new Markup($"  Token:   [bold green]{Markup.Escape(result.Token)}[/]"),
                new Markup($"  Expires: {Formatting.FormatExpiry(result.ExpiresAt.UtcDateTime)}"),
                new Markup("")))
        {
            Header = new PanelHeader("Dashboard Token Created"),
            Border = BoxBorder.Rounded,
        };

        console.Write(panel);

        return 0;
    }
}
