using System.Text.Json;
using Refit;
using Spectre.Console;

namespace CarbonFiles.Cli.Infrastructure;

public static class ErrorHandler
{
    public static int Handle(Exception ex, IAnsiConsole console)
    {
        switch (ex)
        {
            case ApiException apiEx:
                HandleApiException(apiEx, console);
                return 1;

            case HttpRequestException httpEx:
                HandleHttpRequestException(httpEx, console);
                return 1;

            case InvalidOperationException opEx when opEx.Message.Contains("cf config set"):
                console.MarkupLine($"[red]{Markup.Escape(opEx.Message)}[/]");
                return 1;

            default:
                // Check for wrapped ApiException in InnerException
                if (ex.InnerException is ApiException innerApiEx)
                {
                    HandleApiException(innerApiEx, console);
                    return 1;
                }
                console.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
                return 1;
        }
    }

    private static void HandleHttpRequestException(HttpRequestException ex, IAnsiConsole console)
    {
        if (ex.StatusCode.HasValue)
        {
            console.MarkupLine($"[red]HTTP {(int)ex.StatusCode.Value} {ex.StatusCode.Value}[/]");
            // Check inner exception for ApiException with content
            if (ex.InnerException is ApiException innerApiEx)
            {
                HandleApiException(innerApiEx, console);
                return;
            }
        }
        else
        {
            console.MarkupLine($"[red]Connection error:[/] {Markup.Escape(ex.Message)}");
        }

        console.MarkupLine("[dim]Check your server URL with: cf config show[/]");
    }

    private static void HandleApiException(ApiException ex, IAnsiConsole console)
    {
        // Try to parse the CarbonFiles error response: {"error": "...", "hint": "..."}
        if (ex.Content is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(ex.Content);
                var root = doc.RootElement;
                if (root.TryGetProperty("error", out var errorProp))
                {
                    console.MarkupLine($"[red]Error:[/] {Markup.Escape(errorProp.GetString() ?? "")}");
                    if (root.TryGetProperty("hint", out var hintProp))
                    {
                        var hint = hintProp.GetString();
                        if (!string.IsNullOrEmpty(hint))
                            console.MarkupLine($"[dim]Hint: {Markup.Escape(hint)}[/]");
                    }
                    return;
                }
            }
            catch (JsonException) { }
        }

        // Fallback: use status code and reason
        console.MarkupLine($"[red]API Error ({(int)ex.StatusCode} {ex.StatusCode}):[/] {Markup.Escape(ex.Message)}");

        if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized || ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            console.MarkupLine("[dim]Check your auth token with: cf config show[/]");
    }
}
