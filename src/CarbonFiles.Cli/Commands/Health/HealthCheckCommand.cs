using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Health;

public sealed class HealthCheckCommand(ICarbonFilesApi api, IAnsiConsole console)
    : AsyncCommand<GlobalSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellation)
    {
        var health = await api.Healthz(cancellation);

        if (settings.Json)
        {
            console.WriteLine(JsonOutput.Serialize(health));
            return 0;
        }

        if (string.Equals(health.Status, "Healthy", StringComparison.OrdinalIgnoreCase))
        {
            console.MarkupLine("[bold green]Healthy[/]");
        }
        else
        {
            console.MarkupLine($"[bold red]{Markup.Escape(health.Status)}[/]");
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
