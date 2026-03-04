using Spectre.Console;

namespace CarbonFiles.Cli.Infrastructure;

public sealed class VerboseLoggingHandler(IAnsiConsole console) : DelegatingHandler(new HttpClientHandler())
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        console.MarkupLine($"[dim]> {request.Method} {Markup.Escape(request.RequestUri?.ToString() ?? "")}[/]");

        foreach (var header in request.Headers)
        {
            var value = header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                ? MaskAuthHeader(header.Value.FirstOrDefault() ?? "")
                : string.Join(", ", header.Value);
            console.MarkupLine($"[dim]> {Markup.Escape(header.Key)}: {Markup.Escape(value)}[/]");
        }

        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrEmpty(body))
                console.MarkupLine($"[dim]> Body: {Markup.Escape(body)}[/]");
        }

        console.MarkupLine("[dim]>[/]");

        var response = await base.SendAsync(request, cancellationToken);

        console.MarkupLine($"[dim]< {(int)response.StatusCode} {Markup.Escape(response.ReasonPhrase ?? "")}[/]");

        if (response.Content.Headers.ContentType?.MediaType?.Contains("json") == true)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrEmpty(responseBody))
                console.MarkupLine($"[dim]< {Markup.Escape(responseBody)}[/]");
        }

        console.MarkupLine("[dim]<[/]");

        return response;
    }

    private static string MaskAuthHeader(string value)
    {
        if (value.Length <= 12) return "****";
        return value[..12] + "****";
    }
}
