using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Cli.Rendering;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands;

public sealed class UpdateCommand(IAnsiConsole console) : AsyncCommand<UpdateCommand.Settings>
{
    private const string RepoOwner = "carbungo";
    private const string RepoName = "carbon-files-cli";
    private const string ReleasesUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";

    public sealed class Settings : GlobalSettings
    {
        [CommandOption("--check")]
        [Description("Only check for updates without installing.")]
        [DefaultValue(false)]
        public bool CheckOnly { get; init; }

        [CommandOption("-y|--yes")]
        [Description("Skip confirmation prompt.")]
        [DefaultValue(false)]
        public bool Yes { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var currentVersion = BuildInfo.Version;

        console.MarkupLine($"[dim]Current version: {Markup.Escape(BuildInfo.InformationalVersion)}[/]");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"cf-cli/{currentVersion}");

        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync(ReleasesUrl, cancellation);
        }
        catch (HttpRequestException ex)
        {
            console.MarkupLine($"[red]Failed to check for updates:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }

        if (!response.IsSuccessStatusCode)
        {
            console.MarkupLine($"[red]GitHub API returned {(int)response.StatusCode}.[/]");
            return 1;
        }

        var json = await response.Content.ReadAsStringAsync(cancellation);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var tagName = root.GetProperty("tag_name").GetString() ?? "";
        var latestVersion = tagName.TrimStart('v');
        var releaseUrl = root.GetProperty("html_url").GetString() ?? "";

        if (!System.Version.TryParse(latestVersion, out var latest) ||
            !System.Version.TryParse(currentVersion, out var current))
        {
            console.MarkupLine($"[yellow]Could not parse versions (current={currentVersion}, latest={latestVersion}).[/]");
            return 1;
        }

        if (latest <= current)
        {
            console.MarkupLine($"{Theme.Crab} Already on latest.");
            return 0;
        }

        console.MarkupLine($"{Theme.Sparkles} [bold]{Markup.Escape(tagName)} dropped![/]");

        if (settings.CheckOnly)
        {
            console.MarkupLine($"[dim]Release: {Markup.Escape(releaseUrl)}[/]");
            console.MarkupLine($"[dim]Run [bold]cf update[/] to install.[/]");
            return 0;
        }

        var assetName = GetAssetName();
        if (assetName is null)
        {
            console.MarkupLine($"[red]No prebuilt binary available for {RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}.[/]");
            console.MarkupLine("[dim]Install via dotnet: dotnet tool update -g CarbonFiles.Cli[/]");
            return 1;
        }

        // Find the download URL for our asset
        string? downloadUrl = null;
        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            if (asset.GetProperty("name").GetString() == assetName)
            {
                downloadUrl = asset.GetProperty("browser_download_url").GetString();
                break;
            }
        }

        if (downloadUrl is null)
        {
            console.MarkupLine($"[red]Asset '{Markup.Escape(assetName)}' not found in release {Markup.Escape(tagName)}.[/]");
            return 1;
        }

        // Confirm unless --yes
        if (!settings.Yes)
        {
            if (!console.Profile.Capabilities.Interactive)
            {
                console.MarkupLine("[red]Cannot prompt for confirmation in non-interactive mode. Use --yes to confirm.[/]");
                return 1;
            }

            if (!console.Confirm($"Download and install [green]{Markup.Escape(tagName)}[/]?"))
            {
                console.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        // Download the binary
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            console.MarkupLine("[red]Could not determine the current executable path.[/]");
            return 1;
        }

        var tempPath = exePath + ".update";

        try
        {
            await console.Progress().StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"Downloading [blue]{Markup.Escape(assetName)}[/]");
                task.IsIndeterminate = true;

                using var download = await http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellation);
                download.EnsureSuccessStatusCode();

                var totalBytes = download.Content.Headers.ContentLength;
                if (totalBytes.HasValue)
                {
                    task.IsIndeterminate = false;
                    task.MaxValue = totalBytes.Value;
                }

                await using var stream = await download.Content.ReadAsStreamAsync(cancellation);
                await using var fileStream = File.Create(tempPath);

                var buffer = new byte[81920];
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, cancellation)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellation);
                    task.Increment(bytesRead);
                }
            });

            // Replace the running binary
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows, rename current exe out of the way, then move new one in
                var backupPath = exePath + ".old";
                File.Move(exePath, backupPath, overwrite: true);
                File.Move(tempPath, exePath, overwrite: true);
                // The .old file will be left behind — cleaned up on next update
            }
            else
            {
                // On Unix, set executable permission then atomic rename
                File.SetUnixFileMode(tempPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                File.Move(tempPath, exePath, overwrite: true);
            }

            console.MarkupLine($"{Theme.PartyPopper} Updated to [bold green]{Markup.Escape(tagName)}[/]!");
            return 0;
        }
        catch (Exception ex)
        {
            // Clean up temp file on failure
            if (File.Exists(tempPath))
                File.Delete(tempPath);

            console.MarkupLine($"[red]Update failed:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    internal static string? GetAssetName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return RuntimeInformation.OSArchitecture == Architecture.X64 ? "cf-win-x64.exe" : null;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "cf-osx-x64",
                Architecture.Arm64 => "cf-osx-arm64",
                _ => null,
            };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "cf-linux-x64",
                Architecture.Arm64 => "cf-linux-arm64",
                _ => null,
            };
        }

        return null;
    }
}
