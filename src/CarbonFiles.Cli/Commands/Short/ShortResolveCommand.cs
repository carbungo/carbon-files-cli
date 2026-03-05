using System.ComponentModel;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Cli.Rendering;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Short;

public sealed class ShortResolveCommand(ApiClientFactory factory, IAnsiConsole console)
    : AsyncCommand<ShortResolveCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<code>")]
        [Description("Short URL code to resolve.")]
        public string Code { get; init; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var baseClient = factory.CreateHttpClient(settings.Profile);

        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var noRedirectClient = new HttpClient(handler) { BaseAddress = baseClient.BaseAddress };
        noRedirectClient.DefaultRequestHeaders.Authorization = baseClient.DefaultRequestHeaders.Authorization;

        var response = await noRedirectClient.GetAsync($"/s/{settings.Code}", cancellation);

        if (response.StatusCode is System.Net.HttpStatusCode.Found or System.Net.HttpStatusCode.Redirect
            or System.Net.HttpStatusCode.MovedPermanently or System.Net.HttpStatusCode.TemporaryRedirect)
        {
            var location = response.Headers.Location?.ToString();
            console.MarkupLine($"{Theme.Globe} Points to -> {Markup.Escape(location ?? "(unknown)")}");
        }
        else if ((int)response.StatusCode == 404)
        {
            console.MarkupLine($"[red]Short URL code '{Markup.Escape(settings.Code)}' not found.[/]");
            return 1;
        }
        else
        {
            console.MarkupLine($"[yellow]Unexpected response: {(int)response.StatusCode} {response.StatusCode}[/]");
            return 1;
        }

        baseClient.Dispose();
        return 0;
    }
}
