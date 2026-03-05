using CarbonFiles.Client;
using Spectre.Console;

namespace CarbonFiles.Cli.Infrastructure;

public static class ErrorHandler
{
    public static int Handle(Exception ex, IAnsiConsole console)
    {
        switch (ex)
        {
            case CarbonFilesException cfEx:
                HandleCarbonFilesException(cfEx, console);
                return 1;

            case HttpRequestException httpEx:
                HandleHttpRequestException(httpEx, console);
                return 1;

            case InvalidOperationException opEx when opEx.Message.Contains("cf config set"):
                console.MarkupLine($"[red]{Markup.Escape(opEx.Message)}[/]");
                return 1;

            default:
                if (ex.InnerException is CarbonFilesException innerCfEx)
                {
                    HandleCarbonFilesException(innerCfEx, console);
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
            if (ex.InnerException is CarbonFilesException innerCfEx)
            {
                HandleCarbonFilesException(innerCfEx, console);
                return;
            }
        }
        else
        {
            console.MarkupLine($"[red]Connection error:[/] {Markup.Escape(ex.Message)}");
        }

        console.MarkupLine("[dim]Check your server URL with: cf config show[/]");
    }

    private static void HandleCarbonFilesException(CarbonFilesException ex, IAnsiConsole console)
    {
        console.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Error)}");

        if (!string.IsNullOrEmpty(ex.Hint))
            console.MarkupLine($"[dim]Hint: {Markup.Escape(ex.Hint)}[/]");

        if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized || ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            console.MarkupLine("[dim]Check your auth token with: cf config show[/]");
    }
}
