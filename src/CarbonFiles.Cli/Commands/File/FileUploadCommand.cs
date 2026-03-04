using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarbonFiles.Cli.Infrastructure;
using CarbonFiles.Cli.Rendering;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CarbonFiles.Cli.Commands.Files;

public sealed class FileUploadCommand(ApiClientFactory factory, IAnsiConsole console)
    : AsyncCommand<FileUploadCommand.Settings>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

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

    private List<(string LocalPath, string RemotePath)> ResolveFilePaths(Settings settings)
    {
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

                var baseDir = Path.GetFullPath(path);
                foreach (var file in Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(baseDir, file).Replace('\\', '/');
                    files.Add((file, relativePath));
                }
            }
            else if (File.Exists(path))
            {
                files.Add((path, Path.GetFileName(path)));
            }
            else
            {
                console.MarkupLine($"[yellow]Skipping '{Markup.Escape(path)}' (not found).[/]");
            }
        }

        return files;
    }

    private async Task<int> UploadFromStdinAsync(Settings settings, CancellationToken cancellation)
    {
        using var client = CreateHttpClient(settings);

        UploadResult? result = null;
        await console.Progress().StartAsync(async ctx =>
        {
            var task = ctx.AddTask($"Uploading [blue]{Markup.Escape(settings.Name!)}[/] from stdin");
            task.IsIndeterminate = true;

            using var content = new StreamContent(Console.OpenStandardInput());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var encodedFilename = Uri.EscapeDataString(settings.Name!);
            var response = await client.PutAsync(
                $"/api/buckets/{settings.BucketId}/upload/stream?filename={encodedFilename}",
                content,
                cancellation);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellation);
            result = ParseUploadResponse(json);
            task.StopTask();
        });

        if (result is not null)
        {
            PrintSummary(settings.BucketId, settings.Profile, [result]);
        }

        return 0;
    }

    private async Task<int> UploadFilesAsync(Settings settings, List<(string LocalPath, string RemotePath)> files, CancellationToken cancellation)
    {
        using var client = CreateHttpClient(settings);
        var results = new List<UploadResult>();

        await console.Progress().StartAsync(async ctx =>
        {
            foreach (var (localPath, remotePath) in files)
            {
                var fileInfo = new FileInfo(localPath);
                var task = ctx.AddTask($"Uploading [blue]{Markup.Escape(remotePath)}[/]",
                    maxValue: fileInfo.Length);

                await using var fileStream = System.IO.File.OpenRead(localPath);
                using var progressStream = new ProgressStream(fileStream, bytesRead => task.Value = bytesRead);
                using var content = new StreamContent(progressStream);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var encodedFilename = Uri.EscapeDataString(remotePath);
                var response = await client.PutAsync(
                    $"/api/buckets/{settings.BucketId}/upload/stream?filename={encodedFilename}",
                    content,
                    cancellation);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellation);
                var result = ParseUploadResponse(json);
                if (result is not null)
                {
                    results.Add(result);
                }

                task.Value = task.MaxValue;
                task.StopTask();
            }
        });

        PrintSummary(settings.BucketId, settings.Profile, results);
        return 0;
    }

    private HttpClient CreateHttpClient(Settings settings)
    {
        if (settings.Token is not null)
        {
            // Use upload token: create client from profile but override auth
            var client = factory.CreateHttpClient(settings.Profile);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.Token);
            return client;
        }

        return factory.CreateHttpClient(settings.Profile);
    }

    private static UploadResult? ParseUploadResponse(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var uploaded = doc.RootElement.GetProperty("uploaded");
            if (uploaded.GetArrayLength() > 0)
            {
                var file = uploaded[0];
                return new UploadResult
                {
                    Path = file.GetProperty("path").GetString() ?? "",
                    Size = file.GetProperty("size").GetInt64(),
                    MimeType = file.GetProperty("mime_type").GetString() ?? "",
                    ShortUrl = file.TryGetProperty("short_url", out var su) ? su.GetString() : null,
                };
            }
        }
        catch
        {
            // Ignore parse errors
        }
        return null;
    }

    private void PrintSummary(string bucketId, string? profileName, List<UploadResult> results)
    {
        if (results.Count == 0) return;

        var links = new LinkBuilder(factory.GetProfile(profileName));

        console.WriteLine();
        var table = new Table();
        table.AddColumn(new TableColumn("[bold]Path[/]"));
        table.AddColumn(new TableColumn("[bold]Size[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Type[/]"));
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
                linkCol);
        }

        console.Write(table);
        console.MarkupLine($"[green]Uploaded {results.Count} file(s).[/]");
    }

    private sealed class UploadResult
    {
        public string Path { get; init; } = "";
        public long Size { get; init; }
        public string MimeType { get; init; } = "";
        public string? ShortUrl { get; init; }
    }

    /// <summary>
    /// A wrapper stream that reports read progress for Spectre progress bars.
    /// </summary>
    private sealed class ProgressStream(Stream inner, Action<long> onProgress) : Stream
    {
        private long _totalBytesRead;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position
        {
            get => inner.Position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = inner.Read(buffer, offset, count);
            _totalBytesRead += bytesRead;
            onProgress(_totalBytesRead);
            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytesRead = await inner.ReadAsync(buffer, offset, count, cancellationToken);
            _totalBytesRead += bytesRead;
            onProgress(_totalBytesRead);
            return bytesRead;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var bytesRead = await inner.ReadAsync(buffer, cancellationToken);
            _totalBytesRead += bytesRead;
            onProgress(_totalBytesRead);
            return bytesRead;
        }

        public override void Flush() => inner.Flush();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
