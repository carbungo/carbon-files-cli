using Spectre.Console;

namespace CarbonFiles.Cli.Infrastructure;

public sealed class VerboseLoggingHandler(IAnsiConsole console) : DelegatingHandler(new HttpClientHandler())
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Query strings may contain upload tokens; never include them in diagnostics.
        console.MarkupLine($"[dim]> {request.Method} {Markup.Escape(request.RequestUri?.GetLeftPart(UriPartial.Path) ?? "")}[/]");

        foreach (var header in request.Headers)
        {
            var value = header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                ? MaskAuthHeader(header.Value.FirstOrDefault() ?? "")
                : string.Join(", ", header.Value);
            console.MarkupLine($"[dim]> {Markup.Escape(header.Key)}: {Markup.Escape(value)}[/]");
        }

        if (request.Content is not null)
        {
            console.MarkupLine($"[dim]> Content-Type: {Markup.Escape(request.Content.Headers.ContentType?.ToString() ?? "unknown")}[/]");
        }

        console.MarkupLine("[dim]>[/]");

        var response = await base.SendAsync(request, cancellationToken);

        console.MarkupLine($"[dim]< {(int)response.StatusCode} {Markup.Escape(response.ReasonPhrase ?? "")}[/]");

        console.MarkupLine("[dim]<[/]");

        return response;
    }

    private static string MaskAuthHeader(string value)
    {
        return "****";
    }
}
