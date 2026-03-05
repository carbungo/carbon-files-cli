using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Token;

public sealed class TokenInfoCommand(CarbonFilesClient client, IAnsiConsole console)
    : AsyncCommand<TokenInfoCommand.Settings>
{
    public sealed class Settings : GlobalSettings;

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        if (settings.Json)
        {
            var i = await client.Dashboard.GetCurrentUserAsync(cancellation);
            console.WriteLine(JsonOutput.Serialize(i));
            return 0;
        }

        var info = await console.Status().StartAsync($"{Theme.MagnifyingGlass} Fetching token info...", async _ =>
            await client.Dashboard.GetCurrentUserAsync(cancellation));

        var table = Theme.CreateTable();
        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("Scope", Markup.Escape(info.Scope));
        table.AddRow("Expires", Formatting.FormatExpiry(info.ExpiresAt.UtcDateTime));

        console.Write(table);

        return 0;
    }
}
