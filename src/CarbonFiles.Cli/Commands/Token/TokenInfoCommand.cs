using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Token;

public sealed class TokenInfoCommand(ICarbonFilesApi api, IAnsiConsole console)
    : AsyncCommand<TokenInfoCommand.Settings>
{
    public sealed class Settings : GlobalSettings;

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var info = await api.Me(cancellation);

        if (settings.Json)
        {
            console.WriteLine(JsonOutput.Serialize(info));
            return 0;
        }

        var table = new Table { Border = TableBorder.Rounded };
        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("Scope", Markup.Escape(info.Scope));
        table.AddRow("Expires", Formatting.FormatExpiry(info.ExpiresAt.UtcDateTime));

        console.Write(table);

        return 0;
    }
}
