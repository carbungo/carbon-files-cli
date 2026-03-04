using System.ComponentModel;
using CarbonFiles.Cli.Infrastructure;
using Microsoft.AspNetCore.SignalR.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Bucket;

public sealed class BucketWatchCommand(ApiClientFactory factory, IAnsiConsole console)
    : AsyncCommand<BucketWatchCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("Bucket ID to watch for live changes.")]
        public string Id { get; init; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var profile = factory.GetProfile(settings.Profile);
        var hubUrl = $"{profile.Url}/hub/files";

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(profile.Token);
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<object>("FileCreated", data =>
            console.MarkupLine($"[green]+[/] [green]FileCreated[/]: {data}"));
        connection.On<object>("FileUpdated", data =>
            console.MarkupLine($"[yellow]~[/] [yellow]FileUpdated[/]: {data}"));
        connection.On<object>("FileDeleted", data =>
            console.MarkupLine($"[red]-[/] [red]FileDeleted[/]: {data}"));
        connection.On<object>("BucketUpdated", data =>
            console.MarkupLine($"[blue]B[/] [blue]BucketUpdated[/]: {data}"));
        connection.On<object>("BucketDeleted", data =>
            console.MarkupLine($"[red]X[/] [red]BucketDeleted[/]: {data}"));

        await connection.StartAsync(cancellation);

        console.MarkupLine($"[bold]Watching bucket {Markup.Escape(settings.Id)} for changes...[/]");
        console.MarkupLine("[dim]Press Ctrl+C to stop.[/]");

        try
        {
            await Task.Delay(Timeout.Infinite, cancellation);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        finally
        {
            await connection.DisposeAsync();
        }

        return 0;
    }
}
