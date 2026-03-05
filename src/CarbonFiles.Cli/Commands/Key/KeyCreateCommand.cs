using System.ComponentModel;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using CarbonFiles.Client.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Key;

public sealed class KeyCreateCommand(CarbonFilesClient client, IAnsiConsole console)
    : AsyncCommand<KeyCreateCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<name>")]
        [Description("Name for the new API key.")]
        public string Name { get; init; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        if (settings.Json)
        {
            var r = await client.Keys.CreateAsync(new CreateApiKeyRequest { Name = settings.Name }, cancellation);
            console.WriteLine(JsonOutput.Serialize(r));
            return 0;
        }

        var result = await console.Status().StartAsync($"{Theme.Locked} Creating API key...", async _ =>
            await client.Keys.CreateAsync(new CreateApiKeyRequest { Name = settings.Name }, cancellation));

        var panel = new Panel(
            new Rows(
                new Markup(""),
                new Markup($"  Key:    [bold green]{Markup.Escape(result.Key)}[/]"),
                new Markup($"  Prefix: {Markup.Escape(result.Prefix)}"),
                new Markup($"  Name:   {Markup.Escape(result.Name)}"),
                new Markup(""),
                new Markup("  [bold yellow]\u26a0 Save this key \u2014 it won\u2019t be shown again![/]"),
                new Markup("")))
        {
            Header = new PanelHeader($"{Theme.Locked} API Key Created"),
            Border = BoxBorder.Rounded,
        };

        console.Write(panel);

        return 0;
    }
}
