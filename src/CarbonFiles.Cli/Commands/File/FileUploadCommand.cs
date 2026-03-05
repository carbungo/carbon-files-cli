using System.ComponentModel;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Cli.Rendering;
using CarbonFiles.Client;
using CarbonFiles.Client.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Files;

public sealed class FileUploadCommand(ApiClientFactory factory, IAnsiConsole console)
    : AsyncCommand<FileUploadCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<bucket-id>")]
        [Description("Bucket ID to upload files to.")]
        public string BucketId { get; init; } = null!;

        [CommandArgument(1, "[paths]")]
        [Description("File or directory paths to upload.")]
        public string[] Paths { get; init; } = [];

        [CommandOption("-r|--recursive")]
        [Description("Upload directory contents recursively.")]
        [DefaultValue(false)]
        public bool Recursive { get; init; }

        [CommandOption("--stdin")]
        [Description("Read file content from stdin.")]
        [DefaultValue(false)]
        public bool Stdin { get; init; }

        [CommandOption("-n|--name <NAME>")]
        [Description("File name for stdin mode.")]
        public string? Name { get; init; }

        [CommandOption("--token <TOKEN>")]
        [Description("Upload token override (uses this instead of profile token).")]
        public string? Token { get; init; }

        [CommandOption("--base-dir <DIR>")]
        [Description("Base directory for computing relative remote paths (defaults to current directory).")]
        public string? BaseDir { get; init; }

        [CommandOption("--flat")]
        [Description("Strip all directory paths and upload files with filename only.")]
        [DefaultValue(false)]
        public bool Flat { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        if (settings.Stdin)
        {
            if (string.IsNullOrEmpty(settings.Name))
            {
                console.MarkupLine("[red]Error: --name is required when using --stdin.[/]");
                return 1;
            }

            return await UploadFromStdinAsync(settings, cancellation);
        }

        if (settings.Paths.Length == 0)
        {
            console.MarkupLine("[red]Error: Provide at least one file path or use --stdin.[/]");
            return 1;
        }

        var filePaths = ResolveFilePaths(settings);
        if (filePaths.Count == 0)
        {
            console.MarkupLine("[yellow]No files found to upload.[/]");
            return 0;
        }

        return await UploadFilesAsync(settings, filePaths, cancellation);
    }

    internal List<(string LocalPath, string RemotePath)> ResolveFilePaths(Settings settings)
    {
        var baseDir = Path.GetFullPath(settings.BaseDir ?? Directory.GetCurrentDirectory());
        var files = new List<(string LocalPath, string RemotePath)>();

        foreach (var path in settings.Paths)
        {
            if (Directory.Exists(path))
            {
                if (!settings.Recursive)
                {
                    console.MarkupLine($"[yellow]Skipping directory '{Markup.Escape(path)}' (use -r for recursive upload).[/]");
                    continue;
                }

                var dirFullPath = Path.GetFullPath(path);
                foreach (var file in Directory.EnumerateFiles(dirFullPath, "*", SearchOption.AllDirectories))
                {
                    var remotePath = ComputeRemotePath(file, baseDir, settings.Flat);
                    files.Add((file, remotePath));
                }
            }
            else if (File.Exists(path))
            {
                var fullPath = Path.GetFullPath(path);
                var remotePath = ComputeRemotePath(fullPath, baseDir, settings.Flat);
                files.Add((fullPath, remotePath));
            }
            else
            {
                console.MarkupLine($"[yellow]Skipping '{Markup.Escape(path)}' (not found).[/]");
            }
        }

        return files;
    }

    internal static string ComputeRemotePath(string fullPath, string baseDir, bool flat)
    {
        if (flat)
            return Path.GetFileName(fullPath);

        var relativePath = Path.GetRelativePath(baseDir, fullPath).Replace('\\', '/');

        // If the file is outside the base dir, GetRelativePath returns ../.. paths — fall back to filename
        if (relativePath.StartsWith(".."))
            return Path.GetFileName(fullPath);

        return relativePath;
    }

    private async Task<int> UploadFromStdinAsync(Settings settings, CancellationToken cancellation)
    {
        var client = CreateClient(settings);
        var files = client.Buckets[settings.BucketId].Files;

        UploadResponse? result = null;
        await console.Progress().StartAsync(async ctx =>
        {
            var task = ctx.AddTask($"Uploading [blue]{Markup.Escape(settings.Name!)}[/] from stdin");
            task.IsIndeterminate = true;

            result = await files.UploadAsync(
                Console.OpenStandardInput(),
                settings.Name!,
                uploadToken: settings.Token,
                ct: cancellation);

            task.StopTask();
        });

        if (result is not null)
        {
            PrintSummary(settings.BucketId, settings.Profile, result.Uploaded);
        }

        return 0;
    }

    private async Task<int> UploadFilesAsync(Settings settings, List<(string LocalPath, string RemotePath)> filePaths, CancellationToken cancellation)
    {
        var client = CreateClient(settings);
        var files = client.Buckets[settings.BucketId].Files;
        var allUploaded = new List<UploadedFile>();

        await console.Progress().StartAsync(async ctx =>
        {
            foreach (var (localPath, remotePath) in filePaths)
            {
                var fileInfo = new FileInfo(localPath);
                var task = ctx.AddTask($"Uploading [blue]{Markup.Escape(remotePath)}[/]",
                    maxValue: fileInfo.Length);

                var progress = new Progress<UploadProgress>(p => task.Value = p.BytesSent);

                var result = await files.UploadFileAsync(
                    localPath,
                    remotePath,
                    progress,
                    settings.Token,
                    cancellation);

                allUploaded.AddRange(result.Uploaded);
                task.Value = task.MaxValue;
                task.StopTask();
            }
        });

        PrintSummary(settings.BucketId, settings.Profile, allUploaded);
        return 0;
    }

    private CarbonFilesClient CreateClient(Settings settings)
    {
        return factory.Create(settings.Profile);
    }

    private void PrintSummary(string bucketId, string? profileName, List<UploadedFile> results)
    {
        if (results.Count == 0) return;

        var links = new LinkBuilder(factory.GetProfile(profileName));

        console.WriteLine();
        var table = Theme.CreateTable();
        table.AddColumn(new TableColumn("[bold]Path[/]"));
        table.AddColumn(new TableColumn("[bold]Size[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Type[/]"));
        table.AddColumn(new TableColumn("[bold]Dedup[/]"));
        table.AddColumn(new TableColumn(links.HasFrontend ? "[bold]Link[/]" : "[bold]Short URL[/]"));

        foreach (var result in results)
        {
            var linkCol = links.HasFrontend
                ? Markup.Escape(links.FileUrl(bucketId, result.Path))
                : Markup.Escape(result.ShortUrl ?? "-");

            table.AddRow(
                Markup.Escape(result.Path),
                Formatting.FormatSize(result.Size),
                Markup.Escape(result.MimeType),
                result.Deduplicated ? "[cyan]yes[/]" : "-",
                linkCol);
        }

        console.Write(table);

        var dedupCount = results.Count(r => r.Deduplicated);
        var totalSize = results.Sum(r => r.Size);
        var msg = $"{Theme.Package} {results.Count} file(s) landed";
        if (dedupCount > 0)
            msg += $" [dim]({dedupCount} deduped)[/]";
        msg += $" [dim]({Formatting.FormatSize(totalSize)})[/]";
        console.MarkupLine(msg);
    }
}
