using System.Net;
using System.Security.Cryptography;
using System.Text;
using CarbonFiles.Cli.Infrastructure;
using FluentAssertions;

namespace CarbonFiles.Cli.Tests.Infrastructure;

public class DeployServiceTests
{
    [Fact]
    public async Task ScanAndPlan_RejectsLinks_AndOrdersHtmlLast()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "index.html"), "hello");
            File.WriteAllText(Path.Combine(dir.FullName, "app.js"), "script");
            var files = await DeployService.ScanAsync(dir.FullName, default);
            files.Should().HaveCount(2);
            files[0].Sha256.Should().Be(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("script"))));
            var plan = DeployService.MakePlan(files, new Dictionary<string, string?> { ["old.txt"] = "abc" }, true);
            plan.Uploads.Select(f => f.Path).Should().Equal("app.js", "index.html");
            plan.Deletes.Should().Equal("old.txt");
            DeployService.MakePlan(files, new Dictionary<string, string?> { ["old.txt"] = "abc" }, false)
                .Deletes.Should().BeEmpty();
            File.CreateSymbolicLink(Path.Combine(dir.FullName, "link"), Path.Combine(dir.FullName, "app.js"));
            var act = () => DeployService.ScanAsync(dir.FullName, default);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Symlinks*");
        }
        finally { dir.Delete(true); }
    }

    [Fact]
    public async Task Listing_PaginatesAndRetriesTransientResponse()
    {
        var calls = 0;
        var handler = new CallbackHandler(request =>
        {
            calls++;
            if (calls == 1) return JsonResponse("{}", HttpStatusCode.ServiceUnavailable);
            return request.RequestUri!.Query.Contains("offset=0")
                ? JsonResponse("{\"items\":[{\"path\":\"a.txt\",\"sha256\":\"aaa\"}],\"total\":2}")
                : JsonResponse("{\"items\":[{\"path\":\"b.txt\",\"sha256\":\"bbb\"}],\"total\":2}");
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var service = new DeployService(http, "bucket", "cfu_token", _ => { });
        var files = await service.ListRemoteAsync(default);
        calls.Should().Be(3);
        files.Should().HaveCount(2);
        files["b.txt"].Should().Be("bbb");
    }

    [Fact]
    public async Task Deploy_UsesUploadTokenQuery_AndSkipsUnchangedFiles()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "index.html"), "new");
            File.WriteAllText(Path.Combine(dir.FullName, "app.js"), "same");
            var sameHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("same")));
            var requests = new List<(string Method, string Url, string? Auth)>();
            var handler = new CallbackHandler(request =>
            {
                requests.Add((request.Method.Method, request.RequestUri!.ToString(), request.Headers.Authorization?.ToString()));
                return request.Method == HttpMethod.Get
                    ? JsonResponse($$"""{"items":[{"path":"app.js","sha256":"{{sameHash}}"},{"path":"old.txt","sha256":"abc"}],"total":2}""")
                    : request.Method == HttpMethod.Put ? JsonResponse("{}", HttpStatusCode.Created)
                    : JsonResponse("{\"kept\":2,\"deleted\":1}");
            });
            using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
            var output = new List<string>();
            var service = new DeployService(http, "bucket", "cfu_secret", output.Add);
            var result = await service.DeployAsync(dir.FullName, false, true, 4, default);
            result.Uploaded.Should().Be(1);
            result.Skipped.Should().Be(1);
            result.Pruned.Should().Be(1);
            requests.Should().HaveCount(3);
            requests.Should().OnlyContain(r => r.Auth == null);
            requests[1].Url.Should().Contain("token=cfu_secret");
            requests[2].Url.Should().Contain("token=cfu_secret");
            output.Should().NotContain(line => line.Contains("cfu_secret"));
        }
        finally { dir.Delete(true); }
    }

    [Fact]
    public async Task DryRun_DoesNotMutate_AndConflictRetriesOnce()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "index.html"), "new");
            var mutations = 0;
            var syncs = 0;
            var handler = new CallbackHandler(request =>
            {
                if (request.Method == HttpMethod.Get) return JsonResponse("{\"items\":[],\"total\":0}");
                mutations++;
                if (request.Method == HttpMethod.Post && ++syncs == 1) return JsonResponse("{}", HttpStatusCode.Conflict);
                return JsonResponse(request.Method == HttpMethod.Put ? "{}" : "{\"deleted\":0}", request.Method == HttpMethod.Put ? HttpStatusCode.Created : HttpStatusCode.OK);
            });
            using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
            var service = new DeployService(http, "bucket", "api-key", _ => { });
            await service.DeployAsync(dir.FullName, true, true, 2, default);
            mutations.Should().Be(0);
            var result = await service.DeployAsync(dir.FullName, false, true, 2, default);
            syncs.Should().Be(2);
            result.Uploaded.Should().Be(2);
        }
        finally { dir.Delete(true); }
    }

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class CallbackHandler(Func<HttpRequestMessage, HttpResponseMessage> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(callback(request));
    }
}
