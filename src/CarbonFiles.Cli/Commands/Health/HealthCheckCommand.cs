using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Health;

public sealed class HealthCheckCommand(CarbonFilesClient client, IAnsiConsole console)
    : AsyncCommand<GlobalSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellation)
    {
        if (settings.Json)
        {
            var h = await client.Health.CheckAsync(cancellation);
            console.WriteLine(JsonOutput.Serialize(h));
            return 0;
        }

        var health = await console.Status().StartAsync($"{Theme.GreenHeart} Checking vitals...", async _ =>
            await client.Health.CheckAsync(cancellation));

        if (string.Equals(health.Status, "Healthy", StringComparison.OrdinalIgnoreCase))
        {
            console.MarkupLine($"{Theme.GreenHeart} [bold green]All systems go[/]");
        }
        else
        {
            console.MarkupLine($"{Theme.Collision} [bold red]{Markup.Escape(health.Status)}[/]");
        }

        var uptime = TimeSpan.FromSeconds(health.UptimeSeconds);
        var uptimeStr = uptime.Days > 0
            ? $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m"
            : uptime.Hours > 0
                ? $"{uptime.Hours}h {uptime.Minutes}m"
                : uptime.TotalMinutes >= 1
                    ? $"{uptime.Minutes}m"
                    : $"{uptime.Seconds}s";

        console.MarkupLine($"[bold]Uptime:[/]  {uptimeStr}");
        console.MarkupLine($"[bold]DB:[/]     {Markup.Escape(health.Db)}");

        return 0;
    }
}
