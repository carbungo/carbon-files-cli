using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace CarbonFiles.Cli.Infrastructure;

internal sealed record DeployFile(string Path, string LocalPath, string Sha256, long Size);
internal sealed record DeployPlan(IReadOnlyList<DeployFile> Uploads, int Skipped, IReadOnlyList<string> Deletes);
internal sealed record DeployResult(int Uploaded, int Skipped, int Pruned, long Bytes, TimeSpan Elapsed);

internal sealed class DeployService(HttpClient http, string bucket, string token, Action<string> write)
{
    private readonly string _basePath = $"/api/buckets/{Uri.EscapeDataString(bucket)}";
    private readonly bool _uploadToken = ApiClientFactory.IsUploadToken(token);

    internal static async Task<IReadOnlyList<DeployFile>> ScanAsync(string directory, CancellationToken ct)
    {
        var root = Path.GetFullPath(directory);
        if (!Directory.Exists(root)) throw new InvalidOperationException("Deploy directory does not exist.");
        if (IsLink(root)) throw new InvalidOperationException("Deploy directory is a symlink.");
        var files = new List<DeployFile>();
        var dirs = new Stack<string>();
        dirs.Push(root);
        while (dirs.TryPop(out var dir))
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(dir))
            {
                if (IsLink(entry)) throw new InvalidOperationException($"Symlinks are not supported: {Path.GetRelativePath(root, entry)}");
                if (Directory.Exists(entry)) { dirs.Push(entry); continue; }
                if (!File.Exists(entry)) throw new InvalidOperationException($"Unsupported entry: {Path.GetRelativePath(root, entry)}");
                var path = Path.GetRelativePath(root, entry).Replace('\\', '/');
                if (files.Count >= 10000) throw new InvalidOperationException("Deploy directory exceeds 10000 files.");
                await using var stream = File.OpenRead(entry);
                var digest = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, ct));
                files.Add(new DeployFile(path, entry, digest, stream.Length));
            }
        }
        if (files.Count == 0) throw new InvalidOperationException("Deploy directory must contain at least one file.");
        return files.OrderBy(f => f.Path, StringComparer.Ordinal).ToArray();
    }

    private static bool IsLink(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    internal static DeployPlan MakePlan(IReadOnlyList<DeployFile> local, IReadOnlyDictionary<string, string?> remote, bool prune)
    {
        var uploads = local.Where(f => !remote.TryGetValue(f.Path, out var sha) || !string.Equals(sha, f.Sha256, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            .ThenBy(f => f.Path, StringComparer.Ordinal).ToArray();
        var localPaths = local.Select(f => f.Path).ToHashSet(StringComparer.Ordinal);
        var deletes = prune ? remote.Keys.Where(p => !localPaths.Contains(p)).Order(StringComparer.Ordinal).ToArray() : [];
        return new DeployPlan(uploads, local.Count - uploads.Length, deletes);
    }

    internal async Task<Dictionary<string, string?>> ListRemoteAsync(CancellationToken ct)
    {
        var remote = new Dictionary<string, string?>(StringComparer.Ordinal);
        var offset = 0;
        while (true)
        {
            using var response = await SendWithRetryAsync(() => Request(HttpMethod.Get, $"{_basePath}/files?limit=1000&offset={offset}"), ct);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
            var root = doc.RootElement;
            var items = root.GetProperty("items");
            foreach (var item in items.EnumerateArray())
                remote[item.GetProperty("path").GetString()!] = item.TryGetProperty("sha256", out var sha) && sha.ValueKind == JsonValueKind.String ? sha.GetString() : null;
            var count = items.GetArrayLength();
            offset += count;
            if (offset >= root.GetProperty("total").GetInt32()) break;
            if (count == 0) throw new InvalidOperationException("File listing stopped before all remote files were returned.");
        }
        return remote;
    }

    internal async Task<DeployResult> DeployAsync(string directory, bool dryRun, bool prune, int concurrency, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var local = await ScanAsync(directory, ct);
        var remote = await ListRemoteAsync(ct);
        var plan = MakePlan(local, remote, prune);
        write($"Plan: {plan.Uploads.Count} upload, {plan.Skipped} skip, {plan.Deletes.Count} prune.");
        if (dryRun)
        {
            foreach (var file in plan.Uploads) write($"Would upload {file.Path}");
            foreach (var path in plan.Deletes) write($"Would prune {path}");
            return new DeployResult(0, plan.Skipped, 0, 0, stopwatch.Elapsed);
        }

        var uploaded = 0;
        long bytes = 0;
        async Task UploadBatch(IEnumerable<DeployFile> batch)
        {
            await Parallel.ForEachAsync(batch, new ParallelOptions { MaxDegreeOfParallelism = concurrency, CancellationToken = ct }, async (file, cancel) =>
            {
                await UploadAsync(file, cancel);
                Interlocked.Increment(ref uploaded);
                Interlocked.Add(ref bytes, file.Size);
                write($"Uploaded {file.Path} ({uploaded}/{plan.Uploads.Count})");
            });
        }
        await UploadBatch(plan.Uploads.Where(f => !f.Path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)));
        await UploadBatch(plan.Uploads.Where(f => f.Path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)));

        var pruned = 0;
        if (prune)
        {
            var deleted = await SyncAsync(local, ct);
            if (deleted is null)
            {
                write("Sync conflict; re-uploading manifest files and retrying once.");
                await UploadBatch(local.Where(f => !f.Path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)));
                await UploadBatch(local.Where(f => f.Path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)));
                deleted = await SyncAsync(local, ct);
                if (deleted is null) throw new InvalidOperationException("Deploy sync still conflicts after re-upload.");
            }
            pruned = deleted.Value;
        }
        stopwatch.Stop();
        return new DeployResult(uploaded, plan.Skipped, pruned, bytes, stopwatch.Elapsed);
    }

    private async Task UploadAsync(DeployFile file, CancellationToken ct)
    {
        var path = $"{_basePath}/upload/stream?filename={Uri.EscapeDataString(file.Path)}";
        if (_uploadToken) path += $"&token={Uri.EscapeDataString(token)}";
        using var response = await SendWithRetryAsync(() =>
        {
            var request = Request(HttpMethod.Put, path);
            request.Content = new StreamContent(File.OpenRead(file.LocalPath));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return request;
        }, ct);
    }

    private async Task<int?> SyncAsync(IReadOnlyList<DeployFile> files, CancellationToken ct)
    {
        var path = $"{_basePath}/deploy/sync";
        if (_uploadToken) path += $"?token={Uri.EscapeDataString(token)}";
        using var response = await SendWithRetryAsync(() =>
        {
            var request = Request(HttpMethod.Post, path);
            request.Content = JsonContent.Create(new { files = files.Select(f => new { path = f.Path, sha256 = f.Sha256 }).ToArray() });
            return request;
        }, ct, allowConflict: true);
        if (response.StatusCode == HttpStatusCode.Conflict) return null;
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        return doc.RootElement.GetProperty("deleted").GetInt32();
    }

    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        if (!_uploadToken) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<HttpRequestMessage> makeRequest, CancellationToken ct, bool allowConflict = false)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                using var request = makeRequest();
                var response = await http.SendAsync(request, ct);
                if (response.IsSuccessStatusCode || allowConflict && response.StatusCode == HttpStatusCode.Conflict) return response;
                if (attempt < 3 && (response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500))
                {
                    response.Dispose();
                    await Task.Delay(TimeSpan.FromMilliseconds(250 * (1 << attempt)), ct);
                    continue;
                }
                var code = (int)response.StatusCode;
                response.Dispose();
                throw new InvalidOperationException($"CarbonFiles returned HTTP {code}.");
            }
            catch (HttpRequestException) when (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 * (1 << attempt)), ct);
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException("Could not connect to CarbonFiles after retries.");
            }
        }
    }
}
